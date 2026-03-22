using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Shared.DTOs.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IGetOrderByIdUseCase
    {
        Task<OrderDto?> ExecuteAsync(Guid shopId, Guid orderId, CancellationToken cancellationToken = default);
    }

    public class GetOrderByIdUseCase : IGetOrderByIdUseCase
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrderByIdUseCase(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<OrderDto?> ExecuteAsync(Guid shopId, Guid orderId, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order != null && order.ShopId == shopId)
            {
                return MapToDto(order);
            }
            return null;
        }

        private OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                ShopId = order.ShopId,
                BuyerId = order.BuyerId,
                BuyerName = order.Buyer?.FullName ?? order.Buyer?.Username ?? "Unknown",
                TotalAmount = order.TotalAmount,
                ShippingFee = order.ShippingFee,
                PlatformFee = order.PlatformFee,
                OriginalSubtotal = order.OriginalSubtotal,
                DiscountAmount = order.DiscountAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                ShippingCarrier = order.ShippingCarrier,
                TrackingCode = order.TrackingCode,
                ReceiverInfo = order.ReceiverInfo,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                ShippedAt = order.ShippedAt,
                DeliveredAt = order.DeliveredAt,
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancelledAt,
                ShipByDate = order.ShipByDate,
                ReturnDeadline = order.ReturnDeadline,
                CancelReason = order.CancelReason,
                CancelRequestedBy = order.CancelRequestedBy,
                RowVersion = order.RowVersion,
                IsEscrowReleased = order.IsEscrowReleased,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    OrderId = i.OrderId,
                    ProductId = i.ProductId,
                    VariantId = i.VariantId,
                    ProductNameSnapshot = i.ProductNameSnapshot,
                    Quantity = i.Quantity,
                    PriceAtPurchase = i.PriceAtPurchase,
                    TotalLineAmount = i.Quantity * i.PriceAtPurchase,
                    Variant = i.Variant == null ? null : new ProductVariantDto
                    {
                        Id = i.Variant.Id,
                        SkuCode = i.Variant.SkuCode,
                        ImageUrl = i.Variant.ImageUrl,
                        Attributes = i.Variant.Attributes
                    }
                }).ToList()
            };
        }
    }
}
