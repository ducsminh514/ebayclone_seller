using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    // DTO cho Test Buyer Order
    public class CreateBuyerTestOrderRequest
    {
        public Guid VariantId { get; set; }
        public int Quantity { get; set; }
        public string ReceiverInfo { get; set; } = string.Empty;
    }

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
                var newOrder = new Order
                {
                    OrderNumber = "TESTORD" + DateTime.UtcNow.Ticks.ToString().Substring(8),
                    ShopId = product.ShopId,
                    BuyerId = buyerId,
                    ReceiverInfo = request.ReceiverInfo,
                    ShippingFee = 30000, // Gia lap phí ship tĩnh do test
                    PlatformFee = 5000,  // Phí sàn tĩnh do test
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
