using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Shared.DTOs.Common;
using EbayClone.Shared.DTOs.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IGetOrdersUseCase
    {
        Task<IEnumerable<OrderDto>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default);
        
        Task<PagedResult<OrderDto>> ExecutePagedAsync(
            Guid shopId, 
            int pageNumber, 
            int pageSize, 
            string? status = null, 
            string? searchQuery = null, 
            CancellationToken cancellationToken = default);
    }

    public class GetOrdersUseCase : IGetOrdersUseCase
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrdersUseCase(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<IEnumerable<OrderDto>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            var orders = await _orderRepository.GetOrdersByShopIdAsync(shopId, cancellationToken);
            return orders.Select(MapToDto);
        }

        public async Task<PagedResult<OrderDto>> ExecutePagedAsync(
            Guid shopId, 
            int pageNumber, 
            int pageSize, 
            string? status = null, 
            string? searchQuery = null, 
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _orderRepository.GetPagedOrdersByShopIdAsync(
                shopId, pageNumber, pageSize, status, searchQuery, cancellationToken);

            return new PagedResult<OrderDto>
            {
                Items = items.Select(MapToDto),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
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
                    ProductNameSnapshot = i.ProductNameSnapshot,
                    Quantity = i.Quantity,
                    PriceAtPurchase = i.PriceAtPurchase,
                    TotalLineAmount = i.Quantity * i.PriceAtPurchase
                }).ToList()
            };
        }
    }
}
