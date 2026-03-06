using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IGetOrdersUseCase
    {
        Task<IEnumerable<Order>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default);
    }

    public class GetOrdersUseCase : IGetOrdersUseCase
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrdersUseCase(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<IEnumerable<Order>> ExecuteAsync(Guid shopId, CancellationToken cancellationToken = default)
        {
            return await _orderRepository.GetOrdersByShopIdAsync(shopId, cancellationToken);
        }
    }
}
