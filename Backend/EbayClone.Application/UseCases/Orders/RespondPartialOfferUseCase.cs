using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IRespondPartialOfferUseCase
    {
        Task ExecuteAsync(Guid returnId, string buyerDecision, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// GĐ5A: Buyer mock accept/reject partial refund offer từ seller.
    /// buyerDecision: "ACCEPT" hoặc "REJECT"
    /// </summary>
    public class RespondPartialOfferUseCase : IRespondPartialOfferUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderReturnRepository _returnRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _txRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RespondPartialOfferUseCase(
            IOrderRepository orderRepository,
            IOrderReturnRepository returnRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository txRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _returnRepository = returnRepository;
            _walletRepository = walletRepository;
            _txRepository = txRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid returnId, string buyerDecision, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var returnEntity = await _returnRepository.GetByIdAsync(returnId, cancellationToken);
                if (returnEntity == null)
                    throw new ArgumentException("Return request not found.");

                var order = returnEntity.Order;
                if (order == null)
                    throw new InvalidOperationException("Order associated with return not found.");

                switch (buyerDecision.ToUpper())
                {
                    case "ACCEPT":
                        returnEntity.AcceptPartialOffer();

                        var refundAmount = returnEntity.PartialOfferAmount!.Value;
                        var deduction = order.TotalAmount - refundAmount;

                        returnEntity.MarkRefunded(refundAmount, deduction);
                        order.MarkAsPartiallyRefunded();

                        // Trừ tiền ví seller
                        var wallet = await _walletRepository.GetByShopIdAsync(order.ShopId, cancellationToken);
                        if (wallet != null)
                        {
                            var (fromOnHold, fromPending, fromAvailable) = wallet.ProcessRefund(refundAmount);
                            _walletRepository.Update(wallet);

                            var sources = new System.Collections.Generic.List<string>();
                            if (fromOnHold > 0) sources.Add($"Hold: -{fromOnHold:N0}");
                            if (fromPending > 0) sources.Add($"Pending: -{fromPending:N0}");
                            if (fromAvailable > 0) sources.Add($"Available: -{fromAvailable:N0}");
                            var balanceNote = sources.Count > 0 ? $" ({string.Join(", ", sources)})" : "";

                            await _txRepository.AddAsync(new Domain.Entities.WalletTransaction
                            {
                                ShopId = order.ShopId,
                                Amount = -refundAmount,
                                Type = "REFUND",
                                ReferenceId = order.Id,
                                ReferenceType = "ORDER_RETURN",
                                Description = $"Hoàn tiền 1 phần {refundAmount:N0} đ (Buyer accepted offer) — Đơn #{order.OrderNumber}{balanceNote}",
                                BalanceAfter = wallet.TotalBalance
                            }, cancellationToken);
                        }
                        break;

                    case "REJECT":
                        returnEntity.RejectPartialOffer();
                        // Order giữ nguyên — buyer có thể escalate dispute hoặc seller offer lại
                        break;

                    default:
                        throw new ArgumentException("buyerDecision phải là 'ACCEPT' hoặc 'REJECT'.");
                }

                _returnRepository.Update(returnEntity);
                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
