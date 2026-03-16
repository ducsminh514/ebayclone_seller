using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
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
        private readonly IUnitOfWork _unitOfWork;

        public CreateTestOrderUseCase(
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IPolicyRepository policyRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _walletRepository = walletRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _policyRepository = policyRepository;
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
                decimal shippingFee = 0;
                if (product.ShippingPolicyId.HasValue)
                {
                    var shippingPolicy = await _policyRepository.GetShippingPolicyByIdAsync(
                        product.ShippingPolicyId.Value, cancellationToken);
                    if (shippingPolicy != null)
                    {
                        if (shippingPolicy.OfferFreeShipping)
                        {
                            shippingFee = 0; // Seller đã chọn Free Shipping
                        }
                        else
                        {
                            // Parse DomesticServicesJson → lấy Cost service đầu tiên
                            var services = System.Text.Json.JsonSerializer.Deserialize<List<EbayClone.Shared.DTOs.Policies.ShippingServiceDto>>(
                                shippingPolicy.DomesticServicesJson);
                            if (services != null && services.Count > 0)
                            {
                                shippingFee = services[0].IsFreeShipping ? 0 : services[0].Cost;
                            }
                        }
                    }
                }

                // Lấy HandlingTimeDays từ cùng ShippingPolicy (đã query ở bước 2)
                int handlingDays = 3; // fallback nếu không có policy
                if (product.ShippingPolicyId.HasValue)
                {
                    var shipPolicyForHandling = await _policyRepository.GetShippingPolicyByIdAsync(
                        product.ShippingPolicyId.Value, cancellationToken);
                    if (shipPolicyForHandling != null) handlingDays = shipPolicyForHandling.HandlingTimeDays;
                }

                // 3. Tạo đối tượng Order
                var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                var timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");

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

                // 3. Tạo Order Item
                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    VariantId = variant.Id,
                    ProductNameSnapshot = product.Name + " - " + variant.Attributes,
                    Quantity = request.Quantity,
                    PriceAtPurchase = variant.Price
                };

                newOrder.TotalAmount = (orderItem.Quantity * orderItem.PriceAtPurchase) + newOrder.ShippingFee;
                newOrder.Items.Add(orderItem);

                // 4. Lưu DB
                await _orderRepository.AddAsync(newOrder, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken); // Save trước khi MarkAsPaid (vì Status setter private)

                // 5. Mock checkout = buyer đã thanh toán → auto mark PAID
                newOrder.MarkAsPaid();
                newOrder.SetShipByDate(handlingDays);
                _orderRepository.Update(newOrder);

                // 6. Wallet Escrow — ghi nhận doanh thu treo khi PAID (giống UpdateOrderStatusUseCase)
                var wallet = await _walletRepository.GetByShopIdAsync(product.ShopId, cancellationToken);
                if (wallet != null)
                {
                    wallet.AddPending(newOrder.TotalAmount);
                    _walletRepository.Update(wallet);

                    await _walletTransactionRepository.AddAsync(new WalletTransaction
                    {
                        ShopId = product.ShopId,
                        Amount = newOrder.TotalAmount,
                        Type = "ORDER_INCOME",
                        ReferenceId = newOrder.Id,
                        ReferenceType = "ORDER",
                        Description = $"Tạm giữ {newOrder.TotalAmount:N0} đ (Escrow) từ đơn hàng #{newOrder.OrderNumber}",
                        BalanceAfter = wallet.PendingBalance
                    }, cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
