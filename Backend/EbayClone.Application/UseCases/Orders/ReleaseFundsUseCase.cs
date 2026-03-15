using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IReleaseFundsUseCase
    {
        Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
    }

    public class ReleaseFundsUseCase : IReleaseFundsUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _walletTransactionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ReleaseFundsUseCase(
            IOrderRepository orderRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _walletRepository = walletRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var eligibleOrders = await _orderRepository.GetOrdersEligibleForFundReleaseAsync(cancellationToken);
            int releasedCount = 0;

            foreach (var order in eligibleOrders)
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    var wallet = await _walletRepository.GetByShopIdAsync(order.ShopId, cancellationToken);
                    if (wallet != null)
                    {
                        decimal totalDebit = order.TotalAmount;
                        decimal profit = order.TotalAmount - order.PlatformFee;
                        
                        // Chuyển từ Pending sang Available, khấu trừ Platform Fee
                        wallet.ReleaseEscrow(totalDebit, profit);
                        _walletRepository.Update(wallet);

                        // Ghi log giải ngân
                        await _walletTransactionRepository.AddAsync(new WalletTransaction
                        {
                            ShopId = order.ShopId,
                            Amount = profit,
                            Type = "ESCROW_RELEASE",
                            ReferenceId = order.Id,
                            ReferenceType = "ORDER",
                            Description = $"Giải ngân đơn hàng #{order.OrderNumber}. Thực nhận: {profit:N0} đ (Phí sàn: {order.PlatformFee:N0} đ)",
                            BalanceAfter = wallet.AvailableBalance
                        }, cancellationToken);

                        // Cập nhật trạng thái đơn hàng thành COMPLETED để tránh giải ngân lặp lại
                        order.MarkAsCompleted();
                        _orderRepository.Update(order);
                    }

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    releasedCount++;
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    // Log error for specific order but continue with others
                }
            }

            return releasedCount;
        }
    }
}
