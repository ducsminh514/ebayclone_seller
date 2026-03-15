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
        private readonly IUnitOfWork _unitOfWork;

        public CreateTestOrderUseCase(
            IProductRepository productRepository,
            IOrderRepository orderRepository,
            IUnitOfWork unitOfWork)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
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

                // 1. Thực hiện Atomic Reserve Stock (Khóa kho nguyên tử)
                int updatedRows = await _productRepository.ReserveStockAtomicAsync(variant.Id, request.Quantity, cancellationToken);
                if (updatedRows == 0)
                {
                    throw new InvalidOperationException("Not enough available stock or stock changed during transaction (Concurrency Error).");
                }

                // 2. Tạo đối tượng Order
                // FIX Lỗi 3: Sử dụng Guid Part + Timestamp để đảm bảo duy nhất tuyệt đối
                var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                var timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");

                var newOrder = new Order
                {
                    OrderNumber = $"ORD-{timestamp}-{shortGuid}",
                    IdempotencyKey = request.IdempotencyKey,
                    ShopId = product.ShopId,
                    BuyerId = buyerId,
                    ReceiverInfo = request.ReceiverInfo,
                    ShippingFee = 30000, 
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
