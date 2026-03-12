using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Application.DTOs.Finance;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Finance
{
    public class GetSellerFinanceUseCase : IGetSellerFinanceUseCase
    {
        private readonly IShopRepository _shopRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _transactionRepository;

        public GetSellerFinanceUseCase(
            IShopRepository shopRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository transactionRepository)
        {
            _shopRepository = shopRepository;
            _walletRepository = walletRepository;
            _transactionRepository = transactionRepository;
        }

        public async Task<SellerFinanceDto> ExecuteAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var shop = await _shopRepository.GetByUserIdAsync(userId, cancellationToken);
            if (shop == null) throw new InvalidOperationException("Shop not found for this user.");

            var wallet = await _walletRepository.GetByShopIdAsync(shop.Id, cancellationToken);
            if (wallet == null) throw new InvalidOperationException("Wallet not initialized for this shop.");

            var transactions = await _transactionRepository.GetByWalletIdAsync(wallet.Id, cancellationToken);

            return new SellerFinanceDto
            {
                WalletId = wallet.Id,
                AvailableBalance = wallet.AvailableBalance,
                PendingBalance = wallet.PendingBalance,
                Currency = wallet.Currency,
                RecentTransactions = transactions.Select(t => new WalletTransactionDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    Type = t.Type.ToString(),
                    Status = t.Status.ToString(),
                    CreatedAt = t.CreatedAt.UtcDateTime,
                    ReferenceId = t.ReferenceId.ToString(),
                    Description = t.Description
                }).ToList()
            };
        }
    }
}
