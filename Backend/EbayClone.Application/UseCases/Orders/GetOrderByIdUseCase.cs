using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IGetOrderByIdUseCase
    {
        Task<Order?> ExecuteAsync(Guid shopId, Guid orderId, CancellationToken cancellationToken = default);
    }

    public class GetOrderByIdUseCase : IGetOrderByIdUseCase
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrderByIdUseCase(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<Order?> ExecuteAsync(Guid shopId, Guid orderId, CancellationToken cancellationToken = default)
        {
            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order != null && order.ShopId == shopId)
            {
                return order;
            }
            return null;
        }
    }
}
