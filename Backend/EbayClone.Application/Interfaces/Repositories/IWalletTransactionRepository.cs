using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IWalletTransactionRepository
    {
        Task AddAsync(WalletTransaction transaction, CancellationToken cancellationToken = default);
        Task<List<WalletTransaction>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy danh sách giao dịch phân trang, có filter theo type và khoảng ngày.
        /// </summary>
        Task<(List<WalletTransaction> Items, int Total)> GetPagedAsync(
            Guid shopId,
            int page,
            int pageSize,
            string? type = null,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            CancellationToken cancellationToken = default);
    }
}
