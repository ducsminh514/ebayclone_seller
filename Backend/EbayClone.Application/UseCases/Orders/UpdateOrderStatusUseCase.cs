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
        private readonly IWalletTransactionRepository _walletTransactionRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOrderStatusUseCase(
            IOrderRepository orderRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _walletRepository = walletRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _productRepository = productRepository;
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
                    case "READY_TO_SHIP":
                        order.MarkAsPaid();
                        // Trừ kho thật sự và xả khoá
                        foreach(var item in order.Items)
                        {
                            await _productRepository.DeductStockAtomicAsync(item.VariantId, item.Quantity, cancellationToken);
                        }
                        break;
                    case "PROCESSING":
                        order.MarkAsPrintedLabel();
                        break;
                    case "SHIPPED":
                        order.MarkAsShipped(request.ShippingCarrier ?? "Unknown", request.TrackingCode ?? "");
                        break;
                    case "DELIVERED":
                        order.MarkAsDelivered();
                        // eBay Standard Fulfillment: 
                        // 1. Tính phí sàn 5%
                        order.PlatformFee = order.TotalAmount * 0.05m;
                        
                        // 2. Chuyển số dư thực nhận (95%) vào ví Pending
                        var wallet = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                        if (wallet != null) 
                        {
                            decimal profit = order.TotalAmount - order.PlatformFee; 
                            wallet.AddPending(profit);
                            _walletRepository.Update(wallet);

                            // Add Wallet Transaction Log
                            var wt = new WalletTransaction
                            {
                                ShopId = shopId,
                                Amount = profit,
                                Type = "ORDER_INCOME",
                                ReferenceId = order.Id,
                                ReferenceType = "ORDER",
                                Description = $"Cộng {profit} đ vào ví Pending từ đơn hàng #{order.OrderNumber}",
                                BalanceAfter = wallet.PendingBalance
                            };
                            await _walletTransactionRepository.AddAsync(wt, cancellationToken);
                        }
                        break;
                    case "CANCELLED":
                        // Nếu chưa trừ kho thật (chưa PAID), thì xả khoá Reserved
                        if (order.PaymentStatus == "UNPAID")
                        {
                            foreach(var item in order.Items)
                            {
                                await _productRepository.ReleaseReservationAtomicAsync(item.VariantId, item.Quantity, cancellationToken);
                            }
                        }
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
