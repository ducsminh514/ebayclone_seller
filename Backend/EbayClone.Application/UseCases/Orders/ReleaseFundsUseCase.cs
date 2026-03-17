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
                        // ProcessRelease trả về giá trị thực tế — nếu (0,0) tức là Pending đã cạn
                        // (đơn bị refund/cancel trong khi đang chờ release)
                        var (actualDebit, actualCredit) = wallet.ProcessRelease(totalDebit, profit);
                        _walletRepository.Update(wallet);

                        if (actualDebit > 0)
                        {
                            // Log 1: Phí sàn trừ trước
                            decimal actualFee = actualDebit - actualCredit;
                            await _walletTransactionRepository.AddAsync(new WalletTransaction
                            {
                                ShopId = order.ShopId,
                                Amount = -actualFee,
                                Type = "PLATFORM_FEE",
                                ReferenceId = order.Id,
                                ReferenceType = "ORDER",
                                OrderNumber = order.OrderNumber,
                                Description = $"Phí sàn 5% — Đơn hàng #{order.OrderNumber} (thực thu: {actualFee:N0} đ)",
                                BalanceAfter = wallet.TotalBalance
                            }, cancellationToken);

                            // Log 2: Giải ngân sau phí
                            await _walletTransactionRepository.AddAsync(new WalletTransaction
                            {
                                ShopId = order.ShopId,
                                Amount = actualCredit,
                                Type = "ESCROW_RELEASE",
                                ReferenceId = order.Id,
                                ReferenceType = "ORDER",
                                OrderNumber = order.OrderNumber,
                                Description = $"Giải ngân đơn hàng #{order.OrderNumber}. Thực nhận: {actualCredit:N0} đ (phí sàn: {actualFee:N0} đ)",
                                BalanceAfter = wallet.TotalBalance
                            }, cancellationToken);
                        }
                        // actualDebit = 0: đơn đã bị refund trước khi release → không log
                        // (Pending đã được xử lý qua ProcessRefund rồi, không cần làm gì thêm)
                    }

                    // LUÔN đánh dấu IsEscrowReleased=true để FundRelease không chạy lại
                    // kể cả khi actualDebit=0 (đơn đã refund)
                    order.MarkAsCompleted();
                    _orderRepository.Update(order);

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
