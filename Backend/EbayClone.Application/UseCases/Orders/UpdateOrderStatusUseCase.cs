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
        // eBay Managed Payments: phí sàn ~13.25% (FVF). Demo simplified = 5%
        private const decimal PLATFORM_FEE_RATE = 0.05m;

        private readonly IOrderRepository _orderRepository;
        private readonly IOrderCancellationRepository _cancellationRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _walletTransactionRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IVoucherRepository _voucherRepository; // [FIX-CRITICAL-2] Rollback voucher khi CANCEL
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOrderStatusUseCase(
            IOrderRepository orderRepository,
            IOrderCancellationRepository cancellationRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IProductRepository productRepository,
            IPolicyRepository policyRepository,
            IVoucherRepository voucherRepository, // [FIX-CRITICAL-2]
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _cancellationRepository = cancellationRepository;
            _walletRepository = walletRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _productRepository = productRepository;
            _policyRepository = policyRepository;
            _voucherRepository = voucherRepository;
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

                        // [FIX] PlatformFee = 5% trên OriginalSubtotal (chuẩn eBay — tính trên giá gốc trước discount)
                        // PlatformFeeBase: nếu OriginalSubtotal > 0 dùng giá gốc, còn lại fallback về ItemSubtotal (backward compat)
                        order.PlatformFee = order.PlatformFeeBase * PLATFORM_FEE_RATE;
                        break;

                    case "SHIPPED":
                        order.MarkAsShipped(request.ShippingCarrier ?? "Unknown", request.TrackingCode ?? "");
                        break;

                    case "COMPLETED":
                        order.MarkAsCompleted();

                        // eBay Managed Payments: Khi order COMPLETED → giải ngân escrow
                        // Trừ PendingBalance → cộng AvailableBalance (sau phí sàn)
                        // Dùng ProcessRelease() — resilient: không throw nếu Pending < expected (drift)
                        var walletComplete = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                        if (walletComplete != null && order.TotalAmount > 0)
                        {
                            // netPayout = tiền seller nhận thực = TotalAmount - PlatformFee
                            decimal netPayout = order.TotalAmount - order.PlatformFee;
                            if (netPayout <= 0) netPayout = order.TotalAmount; // safety fallback

                            var (actualDebit, actualCredit) = walletComplete.ProcessRelease(
                                totalDebit: order.TotalAmount,
                                availableCredit: netPayout);

                            _walletRepository.Update(walletComplete);

                            if (actualDebit > 0)
                            {
                                var payoutDesc = order.DiscountAmount > 0
                                    ? $"Giải ngân {actualCredit:N0}đ từ #{order.OrderNumber} (sau phí {order.PlatformFee:N0}đ, voucher -{order.DiscountAmount:N0}đ)"
                                    : $"Giải ngân {actualCredit:N0}đ từ #{order.OrderNumber} (sau phí sàn {order.PlatformFee:N0}đ)";

                                await _walletTransactionRepository.AddAsync(new WalletTransaction
                                {
                                    ShopId = shopId,
                                    Amount = actualCredit,
                                    Type = "PAYOUT",
                                    ReferenceId = order.Id,
                                    ReferenceType = "ORDER",
                                    OrderNumber = order.OrderNumber,
                                    Description = payoutDesc,
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
                        // Nếu chưa set (edge case legacy orders) → set lại
                        if (order.PlatformFee == 0)
                        {
                            var itemSubtotal = order.TotalAmount - order.ShippingFee;
                            order.PlatformFee = itemSubtotal * PLATFORM_FEE_RATE;
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
                        var checkedCancelProductIds = new HashSet<Guid>();
                        foreach (var item in order.Items)
                        {
                            var cancelVariant = await _productRepository.GetVariantByIdAsync(item.VariantId, cancellationToken);
                            if (cancelVariant != null && checkedCancelProductIds.Add(cancelVariant.ProductId))
                            {
                                var cancelProd = await _productRepository.GetByIdAsync(cancelVariant.ProductId, cancellationToken);
                                if (cancelProd != null)
                                {
                                    cancelProd.CheckAndUpdateStockStatus();
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
                            // RollbackApplyAsync:
                            // 1. SQL UPDATE giảm UsedCount/UsedBudget (atomic, không để âm)
                            // 2. Xóa VoucherUsage record → SaveChanges gọi bên dưới
                        }
                        break;

                    default:
                        throw new ArgumentException($"Unsupported status update: {request.NewStatus}");
                }

                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
