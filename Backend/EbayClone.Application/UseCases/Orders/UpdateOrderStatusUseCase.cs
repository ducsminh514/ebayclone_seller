using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IUpdateOrderStatusUseCase
    {
        Task<bool> ExecuteAsync(Guid shopId, Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateOrderStatusUseCase : IUpdateOrderStatusUseCase
    {
        /// <summary>
        /// [Phase 2] Dynamic platform fee dựa trên Seller Level.
        /// TOP_RATED = 4.5% (giảm 10%), ABOVE_STANDARD/NEW = 5%, BELOW_STANDARD = 5.5% (tăng 10%).
        /// eBay thật: Final Value Fee ~13.25%, giảm 10% cho Top Rated Plus.
        /// </summary>
        private static decimal GetPlatformFeeRate(string sellerLevel) => sellerLevel switch
        {
            "TOP_RATED" => 0.045m,       // -10% discount (incentive Top Rated)
            "BELOW_STANDARD" => 0.055m,  // +10% surcharge (penalty)
            _ => 0.05m                    // ABOVE_STANDARD / NEW (base)
        };

        private readonly IOrderRepository _orderRepository;
        private readonly IOrderCancellationRepository _cancellationRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _walletTransactionRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IVoucherRepository _voucherRepository;
        private readonly ISellerDefectRepository _sellerDefectRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IOrderNotificationService _orderNotification;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOrderStatusUseCase(
            IOrderRepository orderRepository,
            IOrderCancellationRepository cancellationRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IProductRepository productRepository,
            IPolicyRepository policyRepository,
            IVoucherRepository voucherRepository,
            ISellerDefectRepository sellerDefectRepository,
            IShopRepository shopRepository,
            IOrderNotificationService orderNotification,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _cancellationRepository = cancellationRepository;
            _walletRepository = walletRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _productRepository = productRepository;
            _policyRepository = policyRepository;
            _voucherRepository = voucherRepository;
            _sellerDefectRepository = sellerDefectRepository;
            _shopRepository = shopRepository;
            _orderNotification = orderNotification;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ExecuteAsync(Guid shopId, Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
                
                if (order == null)
                    throw new ArgumentException("Order not found.");
                    
                if (order.ShopId != shopId)
                    throw new UnauthorizedAccessException("You are not authorized to update this order."); // Blocking IDOR

                // Lưu old status trước khi thay đổi (cho notification)
                var oldStatus = order.Status;

                // --- OPTIMISTIC CONCURRENCY CHECK ---
                if (request.RowVersion == null || !request.RowVersion.SequenceEqual(order.RowVersion))
                {
                    throw new InvalidOperationException("Đơn hàng đã được cập nhật bởi một phiên làm việc khác. Vui lòng tải lại trang.");
                }

                switch (request.NewStatus)
                {
                    case "PAID":
                        order.MarkAsPaid();

                        // Lấy HandlingTimeDays từ ShippingPolicy thực tế (seller đã set khi tạo sản phẩm)
                        int handlingDays = 3; // fallback
                        var firstItem = order.Items.FirstOrDefault();
                        if (firstItem != null)
                        {
                            var paidProduct = await _productRepository.GetByIdAsync(firstItem.ProductId, cancellationToken);
                            if (paidProduct?.ShippingPolicyId != null)
                            {
                                var shipPolicy = await _policyRepository.GetShippingPolicyByIdAsync(paidProduct.ShippingPolicyId.Value, cancellationToken);
                                if (shipPolicy != null) handlingDays = shipPolicy.HandlingTimeDays;
                            }
                        }
                        order.SetShipByDate(handlingDays);
                        
                        // NGHIỆP VỤ 2024: Ghi nhận doanh thu treo (Escrow) ngay khi khách TRẢ TIỀN
                        // [H7-NOTE] Guard: MarkAsPaid() throws nếu status ≠ PENDING_PAYMENT
                        // → CreateTestOrderUseCase đã auto-PAID + escrow, nên case này
                        // chỉ chạy cho non-test orders. Không bao giờ duplicate.
                        // Stock đã được trừ tại mock checkout (CreateTestOrderUseCase)
                        var walletPaid = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                        if (walletPaid != null)
                        {
                            walletPaid.AddPending(order.TotalAmount);
                            _walletRepository.Update(walletPaid);

                            await _walletTransactionRepository.AddAsync(new WalletTransaction
                            {
                                ShopId = shopId,
                                Amount = order.TotalAmount,
                                Type = "ORDER_INCOME",
                                ReferenceId = order.Id,
                                ReferenceType = "ORDER",
                                OrderNumber = order.OrderNumber,
                                Description = $"Tạm giữ {order.TotalAmount:N0} đ (Escrow) từ đơn hàng #{order.OrderNumber}",
                                BalanceAfter = walletPaid.TotalBalance
                            }, cancellationToken);
                        }

                        // [PERF] Denormalized: update Shop stats khi PAID + Dynamic PlatformFee
                        var shopPaid = await _shopRepository.GetByIdAsync(shopId, cancellationToken);

                        // [Phase 2] Dynamic PlatformFee dựa trên Seller Level
                        var feeRate = GetPlatformFeeRate(shopPaid?.SellerLevel ?? "NEW");

                        // [Phase 3E] Top Rated Plus: listing qualifies → thêm 10% fee discount
                        // Điều kiện: seller = TOP_RATED + handling ≤ 1 day + return ≥ 30 days free
                        if (shopPaid?.SellerLevel == SellerLevels.TOP_RATED && firstItem != null)
                        {
                            var trpProduct = await _productRepository.GetByIdAsync(firstItem.ProductId, cancellationToken);
                            if (trpProduct != null)
                            {
                                bool qualifiesTRP = false;
                                // Check handling time
                                if (trpProduct.ShippingPolicyId != null)
                                {
                                    var trpShipPolicy = await _policyRepository.GetShippingPolicyByIdAsync(trpProduct.ShippingPolicyId.Value, cancellationToken);
                                    if (trpShipPolicy != null && trpShipPolicy.HandlingTimeDays <= 1)
                                    {
                                        // Check return policy
                                        if (trpProduct.ReturnPolicyId != null)
                                        {
                                            var trpReturnPolicy = await _policyRepository.GetReturnPolicyByIdAsync(trpProduct.ReturnPolicyId.Value, cancellationToken);
                                            if (trpReturnPolicy != null && trpReturnPolicy.DomesticReturnDays >= 30 && trpReturnPolicy.DomesticShippingPaidBy == "SELLER")
                                            {
                                                qualifiesTRP = true;
                                            }
                                        }
                                    }
                                }

                                if (qualifiesTRP)
                                {
                                    feeRate *= 0.90m; // 10% additional discount (4.5% → 4.05%)
                                }
                            }
                        }

                        order.PlatformFee = order.PlatformFeeBase * feeRate;

                        if (shopPaid != null)
                        {
                            shopPaid.TotalTransactions++;
                            // [FIX-W3] Dùng ItemSubtotal (TotalAmount - ShippingFee) thay vì TotalAmount
                            // Doanh số bán hàng không bao gồm phí ship
                            shopPaid.TotalSalesAmount += order.ItemSubtotal;
                            shopPaid.AwaitingShipmentCount++;
                            _shopRepository.Update(shopPaid);
                        }
                        break;

                    case "SHIPPED":
                        order.MarkAsShipped(request.ShippingCarrier ?? "Unknown", request.TrackingCode ?? "");

                        // [PERF] Denormalized: giảm AwaitingShipmentCount
                        var shopShipped = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
                        if (shopShipped != null)
                        {
                            shopShipped.AwaitingShipmentCount = Math.Max(0, shopShipped.AwaitingShipmentCount - 1);

                            // [DEFECT] Late Shipment detection — tự động check bởi MarkAsShipped()
                            if (order.IsLateShipment)
                            {
                                shopShipped.LateShipmentCount++;
                                await _sellerDefectRepository.AddAsync(new SellerDefect
                                {
                                    ShopId = shopId,
                                    OrderId = order.Id,
                                    BuyerId = order.BuyerId,
                                    DefectType = DefectTypes.LATE_SHIPMENT,
                                    Description = $"Late shipment: shipped {order.ShippedAt:yyyy-MM-dd} > deadline {order.ShipByDate:yyyy-MM-dd}"
                                }, cancellationToken);
                            }
                            _shopRepository.Update(shopShipped);
                        }
                        break;

                    case "COMPLETED":
                        // [FIX-F5] Enforce hold period theo SellerLevel
                        if (order.Status == "DELIVERED" && order.DeliveredAt.HasValue)
                        {
                            var shopHold = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
                            int holdDays = (shopHold?.SellerLevel ?? "NEW") switch
                            {
                                "TOP_RATED" => 0,
                                "ABOVE_STANDARD" => 3,
                                "BELOW_STANDARD" => 14,
                                _ => 21
                            };
                            if (holdDays > 0)
                            {
                                var holdUntil = order.DeliveredAt.Value.AddDays(holdDays);
                                if (DateTimeOffset.UtcNow < holdUntil)
                                    throw new InvalidOperationException(
                                        $"Chưa hết thời gian giữ tiền ({holdDays} ngày). Giải ngân sau: {holdUntil:yyyy-MM-dd HH:mm} UTC.");
                            }
                        }

                        order.MarkAsCompleted();

                        // [FIX-F1] Log 2 transactions nhất quán với ReleaseFundsUseCase
                        var walletComplete = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                        if (walletComplete != null && order.TotalAmount > 0)
                        {
                            decimal netPayout = order.TotalAmount - order.PlatformFee;
                            if (netPayout <= 0) netPayout = order.TotalAmount;

                            var (actualDebit, actualCredit) = walletComplete.ProcessRelease(
                                totalDebit: order.TotalAmount,
                                availableCredit: netPayout);

                            _walletRepository.Update(walletComplete);

                            if (actualDebit > 0)
                            {
                                decimal actualFee = actualDebit - actualCredit;

                                await _walletTransactionRepository.AddAsync(new WalletTransaction
                                {
                                    ShopId = shopId,
                                    Amount = -actualFee,
                                    Type = "PLATFORM_FEE",
                                    ReferenceId = order.Id,
                                    ReferenceType = "ORDER",
                                    OrderNumber = order.OrderNumber,
                                    Description = $"Phí sàn — Đơn #{order.OrderNumber} (thực thu: {actualFee:N0} đ)",
                                    BalanceAfter = walletComplete.TotalBalance
                                }, cancellationToken);

                                await _walletTransactionRepository.AddAsync(new WalletTransaction
                                {
                                    ShopId = shopId,
                                    Amount = actualCredit,
                                    Type = "ESCROW_RELEASE",
                                    ReferenceId = order.Id,
                                    ReferenceType = "ORDER",
                                    OrderNumber = order.OrderNumber,
                                    Description = $"Giải ngân #{order.OrderNumber}. Thực nhận: {actualCredit:N0} đ (phí: {actualFee:N0} đ)",
                                    BalanceAfter = walletComplete.TotalBalance
                                }, cancellationToken);
                            }
                        }
                        break;

                    case "DELIVERED":
                        order.MarkAsDelivered();

                        // Lấy DomesticReturnDays từ ReturnPolicy thực tế (seller đã set khi tạo sản phẩm)
                        int returnDays = 30; // fallback
                        var deliveredItem = order.Items.FirstOrDefault();
                        if (deliveredItem != null)
                        {
                            var deliveredProduct = await _productRepository.GetByIdAsync(deliveredItem.ProductId, cancellationToken);
                            if (deliveredProduct?.ReturnPolicyId != null)
                            {
                                var retPolicy = await _policyRepository.GetReturnPolicyByIdAsync(deliveredProduct.ReturnPolicyId.Value, cancellationToken);
                                if (retPolicy != null)
                                {
                                    returnDays = retPolicy.IsDomesticAccepted ? retPolicy.DomesticReturnDays : 0;
                                }
                            }
                        }
                        if (returnDays > 0) order.SetReturnDeadline(returnDays);

                        // [FIX-M7] PlatformFee đã set ở PAID — verify consistency tại DELIVERED
                        // Nếu chưa set (edge case legacy orders) → set lại với dynamic fee
                        if (order.PlatformFee == 0)
                        {
                            var shopDelivered = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
                            var deliveredFeeRate = GetPlatformFeeRate(shopDelivered?.SellerLevel ?? "NEW");
                            var itemSubtotal = order.TotalAmount - order.ShippingFee;
                            order.PlatformFee = itemSubtotal * deliveredFeeRate;
                        }
                        break;

                    case "CANCELLED":
                        // ⚠️ QUAN TRỌNG: Validate + đổi status TRƯỚC khi side-effects
                        var cancelReason = request.CancelReason ?? "BUYER_ASKED";
                        var cancelRequestedBy = request.CancelRequestedBy ?? "SELLER";
                        order.CancelOrder(cancelReason, cancelRequestedBy);

                        // [BUG-1 FIX] Check xem đã có OrderCancellation (buyer request → seller accepted) chưa
                        // Nếu đã có (ACCEPTED/COMPLETED) thì SKIP tạo mới, tránh duplicate
                        var existingCancellation = await _cancellationRepository.GetByOrderIdAsync(order.Id, cancellationToken);
                        if (existingCancellation == null || existingCancellation.Status == "DECLINED")
                        {
                            // Chưa có hoặc bị decline trước đó → Tạo record mới (seller cancel chủ động)
                            var cancellation = new OrderCancellation
                            {
                                OrderId = order.Id,
                                RequestedBy = cancelRequestedBy,
                                Reason = cancelReason,
                                Notes = request.CancelNotes
                            };
                            cancellation.Initialize(); // Auto-set deadline + defect + fee credit
                            cancellation.Accept();     // Auto-accept vì cancel trực tiếp
                            cancellation.MarkCompleted();
                            await _cancellationRepository.AddAsync(cancellation, cancellationToken);
                        }
                        // else: record đã tồn tại (buyer → seller accepted) → không tạo thêm

                        // [FIX-L5] IsFeeCredited: nếu buyer unpaid cancel → hoàn PlatformFee cho seller
                        // (eBay rule: seller không chịu phí sàn cho đơn buyer không thanh toán)
                        var activeCancellation = existingCancellation ?? 
                            (await _cancellationRepository.GetByOrderIdAsync(order.Id, cancellationToken));
                        if (activeCancellation != null && activeCancellation.IsFeeCredited && order.PlatformFee > 0)
                        {
                            var walletFeeCredit = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                            if (walletFeeCredit != null)
                            {
                                walletFeeCredit.AddAvailable(order.PlatformFee);
                                _walletRepository.Update(walletFeeCredit);

                                await _walletTransactionRepository.AddAsync(new WalletTransaction
                                {
                                    ShopId = shopId,
                                    Amount = order.PlatformFee,
                                    Type = "FEE_CREDIT",
                                    ReferenceId = order.Id,
                                    ReferenceType = "ORDER",
                                    OrderNumber = order.OrderNumber,
                                    Description = $"Hoàn phí sàn {order.PlatformFee:N0} đ (buyer unpaid cancel #{order.OrderNumber})",
                                    BalanceAfter = walletFeeCredit.TotalBalance
                                }, cancellationToken);
                            }
                        }

                        // 1. Hoàn trả ví Pending (chỉ khi đã thanh toán)
                        if (order.PaymentStatus == "PAID")
                        {
                            var walletRefund = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                            if (walletRefund != null)
                            {
                                var (fromOnHold, fromPending, fromAvailable) = walletRefund.ProcessRefund(order.TotalAmount);
                                _walletRepository.Update(walletRefund);

                                var sources = new System.Collections.Generic.List<string>();
                                if (fromOnHold > 0) sources.Add($"Hold: -{fromOnHold:N0}");
                                if (fromPending > 0) sources.Add($"Pending: -{fromPending:N0}");
                                if (fromAvailable > 0) sources.Add($"Available: -{fromAvailable:N0}");
                                var balanceNote = sources.Count > 0 ? $" ({string.Join(", ", sources)})" : "";

                                await _walletTransactionRepository.AddAsync(new WalletTransaction
                                {
                                    ShopId = shopId,
                                    Amount = -order.TotalAmount,
                                    Type = "REFUND",
                                    ReferenceId = order.Id,
                                    ReferenceType = "ORDER",
                                    OrderNumber = order.OrderNumber,
                                    Description = $"Hoàn tiền tạm giữ {order.TotalAmount:N0} đ cho Buyer (Hủy đơn #{order.OrderNumber}){balanceNote}",
                                    BalanceAfter = walletRefund.PendingBalance + walletRefund.AvailableBalance + walletRefund.OnHoldBalance
                                }, cancellationToken);
                            }
                        }

                        // 2. Hoàn kho (Restock) — stock đã trừ trực tiếp tại mock checkout
                        foreach(var item in order.Items)
                        {
                            await _productRepository.RestoreStockAtomicAsync(item.VariantId, item.Quantity, cancellationToken);
                        }

                        // [A6] Sau restock → check auto OUT_OF_STOCK → ACTIVE
                        // [FIX-S1] Track ActiveListingCount delta khi product status thay đổi
                        var checkedCancelProductIds = new HashSet<Guid>();
                        int activeListingDelta = 0;
                        foreach (var item in order.Items)
                        {
                            var cancelVariant = await _productRepository.GetVariantByIdAsync(item.VariantId, cancellationToken);
                            if (cancelVariant != null && checkedCancelProductIds.Add(cancelVariant.ProductId))
                            {
                                var cancelProd = await _productRepository.GetByIdAsync(cancelVariant.ProductId, cancellationToken);
                                if (cancelProd != null)
                                {
                                    var oldProductStatus = cancelProd.Status;
                                    cancelProd.CheckAndUpdateStockStatus();
                                    // [FIX-S1] Track ACTIVE↔OUT_OF_STOCK transitions
                                    if (oldProductStatus != cancelProd.Status)
                                    {
                                        if (cancelProd.Status == "ACTIVE") activeListingDelta++;
                                        else if (oldProductStatus == "ACTIVE") activeListingDelta--;
                                    }
                                    await _productRepository.UpdateAsync(cancelProd, cancellationToken);
                                }
                            }
                        }

                        // [FIX-CRITICAL-2] Hoàn lượt voucher nếu đơn hàng có dùng voucher
                        // Compensating Transaction (Saga pattern): reverse các side-effects của AtomicApply
                        if (order.VoucherId.HasValue && order.DiscountAmount > 0)
                        {
                            await _voucherRepository.RollbackApplyAsync(
                                order.VoucherId.Value,
                                order.DiscountAmount,
                                order.Id);
                        }

                        // [PERF+FIX] Gom tất cả Shop updates (defect + awaiting count) thành 1 lần GetByIdAsync
                        if (cancelReason == "OUT_OF_STOCK" || order.PaymentStatus == "PAID" || activeListingDelta != 0)
                        {
                            var shopCancel = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
                            if (shopCancel != null)
                            {
                                // [DEFECT] Cancel OUT_OF_STOCK → ghi SellerDefect + update Shop.DefectCount
                                if (cancelReason == "OUT_OF_STOCK")
                                {
                                    shopCancel.DefectCount++;
                                    await _sellerDefectRepository.AddAsync(new SellerDefect
                                    {
                                        ShopId = shopId,
                                        OrderId = order.Id,
                                        BuyerId = order.BuyerId,
                                        DefectType = DefectTypes.CANCEL_OUT_OF_STOCK,
                                        Description = $"Seller cancelled order #{order.OrderNumber} — OUT_OF_STOCK"
                                    }, cancellationToken);
                                }

                                // [PERF] Denormalized: giảm AwaitingShipmentCount nếu đơn đang PAID
                                if (order.PaymentStatus == "PAID")
                                {
                                    shopCancel.AwaitingShipmentCount = Math.Max(0, shopCancel.AwaitingShipmentCount - 1);
                                    // [FIX-W1] Giảm TotalTransactions khi cancel đơn đã PAID
                                    shopCancel.TotalTransactions = Math.Max(0, shopCancel.TotalTransactions - 1);
                                    // [FIX-W2] Giảm TotalSalesAmount (dùng ItemSubtotal, khớp với PAID logic)
                                    shopCancel.TotalSalesAmount = Math.Max(0, shopCancel.TotalSalesAmount - order.ItemSubtotal);
                                }

                                // [FIX-S1] Sync ActiveListingCount khi product ACTIVE↔OUT_OF_STOCK
                                if (activeListingDelta != 0)
                                {
                                    shopCancel.ActiveListingCount = Math.Max(0, shopCancel.ActiveListingCount + activeListingDelta);
                                }

                                _shopRepository.Update(shopCancel);
                            }
                        }
                        break;

                    default:
                        throw new ArgumentException($"Unsupported status update: {request.NewStatus}");
                }

                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // [SignalR] Push notification SAU commit
                await _orderNotification.NotifyOrderStatusChangedAsync(
                    shopId, orderId, order.OrderNumber, oldStatus, request.NewStatus);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
