using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Orders;
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
        private readonly IOrderRepository _orderRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOrderStatusUseCase(
            IOrderRepository orderRepository,
            ISellerWalletRepository walletRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _walletRepository = walletRepository;
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

                switch (request.NewStatus)
                {
                    case "PAID_READY_TO_SHIP":
                        order.MarkAsPaid();
                        break;
                    case "PRINTED_LABEL":
                        order.MarkAsPrintedLabel();
                        break;
                    case "SHIPPED":
                        order.MarkAsShipped(request.ShippingCarrier ?? "Unknown", request.TrackingCode ?? "");
                        break;
                    case "DELIVERED":
                        order.MarkAsDelivered();
                        // Tính năng Fulfillment: Tự động cộng PendingBalance cho Seller khi khách nhận xong.
                        var wallet = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                        if (wallet != null) 
                        {
                            decimal profit = order.TotalAmount - order.PlatformFee; // Coi như đã trừ các phí.
                            wallet.AddPending(profit);
                            _walletRepository.Update(wallet);
                        }
                        break;
                    case "CANCELLED":
                        order.CancelOrder();
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
