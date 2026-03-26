using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Application.UseCases.Vouchers;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{

    public interface ICreateTestOrderUseCase
    {
        Task<Guid> ExecuteAsync(Guid buyerId, CreateBuyerTestOrderRequest request, CancellationToken cancellationToken = default);
    }

    public class CreateTestOrderUseCase : ICreateTestOrderUseCase
    {
        private readonly IProductRepository _productRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _walletTransactionRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IVoucherRepository _voucherRepository;
        private readonly IShopRepository _shopRepository;
        private readonly ApplyVoucherUseCase _applyVoucherUseCase;
        private readonly IOrderNotificationService _orderNotification;
        private readonly IUnitOfWork _unitOfWork;

        public CreateTestOrderUseCase(
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IPolicyRepository policyRepository,
            IVoucherRepository voucherRepository,
            IShopRepository shopRepository,
            ApplyVoucherUseCase applyVoucherUseCase,
            IOrderNotificationService orderNotification,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _walletRepository = walletRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _policyRepository = policyRepository;
            _voucherRepository = voucherRepository;
            _shopRepository = shopRepository;
            _applyVoucherUseCase = applyVoucherUseCase;
            _orderNotification = orderNotification;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(Guid buyerId, CreateBuyerTestOrderRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // 0. Check Idempotency (eBay Standard) - INSIDE Transaction to prevent Race Condition
                var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
                if (existingOrder != null) 
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return existingOrder.Id;
                }

                var variant = await _productRepository.GetVariantByIdAsync(request.VariantId, cancellationToken);
                if (variant == null)
                    throw new ArgumentException("Variant not found");

                var product = await _productRepository.GetByIdAsync(variant.ProductId, cancellationToken);
                if (product == null)
                    throw new ArgumentException("Product not found");
                
                // [Security] Không cho đặt đơn nếu sản phẩm không ACTIVE
                if (product.Status != "ACTIVE")
                    throw new InvalidOperationException($"Sản phẩm '{product.Name}' hiện ở trạng thái '{product.Status}', không thể đặt mua.");

                // [FIX-M1+M2] Check PaymentPolicy config
                int paymentDaysToPayment = 0; // 0 = immediate (default)
                if (product.PaymentPolicyId.HasValue)
                {
                    var paymentPolicy = await _policyRepository.GetPaymentPolicyByIdAsync(
                        product.PaymentPolicyId.Value, cancellationToken);
                    if (paymentPolicy != null)
                    {
                        // [M1] ImmediatePaymentRequired enforcement
                        // Mock order luôn auto-pay, nhưng ghi nhận config cho real checkout
                        if (!paymentPolicy.ImmediatePaymentRequired)
                        {
                            paymentDaysToPayment = paymentPolicy.DaysToPayment;
                        }
                        // Nếu ImmediatePaymentRequired = true → paymentDaysToPayment = 0 (auto-pay below)
                    }
                }
                // 1. Thực hiện Atomic Deduct Stock (Trừ kho trực tiếp — single-step model)
                int updatedRows = await _productRepository.DeductStockAtomicAsync(variant.Id, request.Quantity, cancellationToken);
                if (updatedRows == 0)
                {
                    throw new InvalidOperationException("Không đủ hàng tồn kho hoặc xung đột đồng thời (Concurrency Error).");
                }

                // [A6] Auto OUT_OF_STOCK nếu hết hàng sau deduct
                var updatedProduct = await _productRepository.GetByIdAsync(variant.ProductId, cancellationToken);
                if (updatedProduct != null)
                {
                    updatedProduct.CheckAndUpdateStockStatus();
                    await _productRepository.UpdateAsync(updatedProduct, cancellationToken);
                }

                // 2. Tính phí ship từ ShippingPolicy thực tế (seller đã set khi tạo sản phẩm)
                // [FIX-H1+H2+L4] Query 1 lần, tính AdditionalItemCost cho Qty > 1
                decimal shippingFee = 0;
                int handlingDays = 3; // fallback nếu không có policy
                if (product.ShippingPolicyId.HasValue)
                {
                    var shippingPolicy = await _policyRepository.GetShippingPolicyByIdAsync(
                        product.ShippingPolicyId.Value, cancellationToken);
                    if (shippingPolicy != null)
                    {
                        // [L4] Reuse query — lấy HandlingTimeDays luôn
                        handlingDays = shippingPolicy.HandlingTimeDays;

                        if (shippingPolicy.OfferFreeShipping)
                        {
                            shippingFee = 0; // Seller đã chọn Free Shipping
                        }
                        else
                        {
                            // Parse DomesticServicesJson → lấy service đầu tiên
                            // TODO: Khi có checkout UI → buyer chọn service, truyền serviceIndex
                            var services = System.Text.Json.JsonSerializer.Deserialize<List<EbayClone.Shared.DTOs.Policies.ShippingServiceDto>>(
                                shippingPolicy.DomesticServicesJson);
                            if (services != null && services.Count > 0)
                            {
                                var svc = services[0];
                                if (svc.IsFreeShipping)
                                {
                                    shippingFee = 0;
                                }
                                else
                                {
                                    // [H1+H2] eBay rule: Item 1 = Cost, item 2+ = AdditionalItemCost
                                    // VD: Cost=10, AdditionalItemCost=3, Qty=3 → 10 + 3 + 3 = 16
                                    shippingFee = svc.Cost + (svc.AdditionalItemCost * Math.Max(0, request.Quantity - 1));
                                }
                            }
                        }
                    }
                }

                // 3. Tạo đối tượng Order
                var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                var timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");

                // Tính OriginalSubtotal (giá gốc trước discount) — dùng cho PlatformFee
                decimal itemSubtotal = 0; // sẽ tính sau khi có OrderItem

                var newOrder = new Order
                {
                    OrderNumber = $"ORD-{timestamp}-{shortGuid}",
                    IdempotencyKey = request.IdempotencyKey,
                    ShopId = product.ShopId,
                    BuyerId = buyerId,
                    ReceiverInfo = request.ReceiverInfo,
                    ShippingFee = shippingFee,
                    PlatformFee = 0
                };

                // 3b. Tạo Order Item
                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    VariantId = variant.Id,
                    ProductNameSnapshot = product.Name + " - " + variant.Attributes,
                    Quantity = request.Quantity,
                    PriceAtPurchase = variant.Price
                };

                itemSubtotal = orderItem.Quantity * orderItem.PriceAtPurchase;
                newOrder.OriginalSubtotal = itemSubtotal; // Lưu giá gốc trước discount
                newOrder.Items.Add(orderItem);

                // 3c. Apply Voucher (optional)
                Guid? appliedVoucherId = null;
                decimal discountAmount = 0;
                if (!string.IsNullOrWhiteSpace(request.VoucherCode))
                {
                    // [FIX-HIGH-3] Dùng injected UseCase thay vì tự new
                    var productIdsInCart = new List<Guid> { product.Id };
                    var voucherResult = await _applyVoucherUseCase.ExecuteAsync(
                        request.VoucherCode,
                        product.ShopId,
                        buyerId,
                        itemSubtotal,
                        productIdsInCart);
                    discountAmount = voucherResult.DiscountAmount;
                    appliedVoucherId = voucherResult.VoucherId;
                    newOrder.VoucherId = appliedVoucherId;
                    newOrder.DiscountAmount = discountAmount;
                }

                // TotalAmount = OriginalSubtotal + ShippingFee - Discount
                newOrder.TotalAmount = itemSubtotal + shippingFee - discountAmount;

                // 4. Lưu DB
                await _orderRepository.AddAsync(newOrder, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken); // Save trước khi MarkAsPaid (vì Status setter private)

                // 5. Mock checkout = buyer đã thanh toán → auto mark PAID
                newOrder.MarkAsPaid();
                newOrder.SetShipByDate(handlingDays);

                // [Phase 2] Dynamic PlatformFee dựa trên Seller Level
                var shopForFee = await _shopRepository.GetByIdAsync(product.ShopId, cancellationToken);
                var feeRate = shopForFee?.SellerLevel switch
                {
                    "TOP_RATED" => 0.045m,
                    "BELOW_STANDARD" => 0.055m,
                    _ => 0.05m
                };
                newOrder.PlatformFee = newOrder.PlatformFeeBase * feeRate;

                _orderRepository.Update(newOrder);

                // 6b. Ghi VoucherUsage nếu có dùng voucher
                if (appliedVoucherId.HasValue)
                {
                    await _voucherRepository.AddUsageAsync(new VoucherUsage
                    {
                        VoucherId = appliedVoucherId.Value,
                        BuyerId = buyerId,
                        OrderId = newOrder.Id,
                        DiscountAmount = discountAmount
                    });
                }

                // 6. Wallet Escrow — ghi nhận doanh thu treo khi PAID (giống UpdateOrderStatusUseCase)
                var wallet = await _walletRepository.GetByShopIdAsync(product.ShopId, cancellationToken);
                if (wallet != null)
                {
                    wallet.AddPending(newOrder.TotalAmount);
                    _walletRepository.Update(wallet);

                    // [FIX-MEDIUM] Enrich description với discount info để Finance audit rõ hơn
                    var txDescription = newOrder.DiscountAmount > 0
                        ? $"Tạm giữ {newOrder.TotalAmount:N0}đ từ #{newOrder.OrderNumber} (Voucher -{newOrder.DiscountAmount:N0}đ, gốc: {newOrder.OriginalSubtotal:N0}đ)"
                        : $"Tạm giữ {newOrder.TotalAmount:N0}đ (Escrow) từ đơn hàng #{newOrder.OrderNumber}";

                    await _walletTransactionRepository.AddAsync(new WalletTransaction
                    {
                        ShopId = product.ShopId,
                        Amount = newOrder.TotalAmount,
                        Type = "ORDER_INCOME",
                        ReferenceId = newOrder.Id,
                        ReferenceType = "ORDER",
                        Description = txDescription,
                        BalanceAfter = wallet.TotalBalance
                    }, cancellationToken);
                }

                // [PERF] Denormalized: update Shop stats khi mock order auto-PAID
                // (Dùng shopForFee đã load ở trên — tránh duplicate query)
                if (shopForFee != null)
                {
                    shopForFee.TotalTransactions++;
                    // [FIX-W3] Dùng ItemSubtotal (không bao gồm ShippingFee)
                    shopForFee.TotalSalesAmount += newOrder.ItemSubtotal;
                    shopForFee.AwaitingShipmentCount++;
                    _shopRepository.Update(shopForFee);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // [SignalR] Push notification SAU commit — tránh phantom notification
                await _orderNotification.NotifyNewOrderAsync(
                    newOrder.ShopId, newOrder.Id, newOrder.OrderNumber, newOrder.TotalAmount);

                return newOrder.Id;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
