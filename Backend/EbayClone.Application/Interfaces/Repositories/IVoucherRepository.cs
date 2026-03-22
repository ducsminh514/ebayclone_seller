using EbayClone.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EbayClone.Application.Interfaces.Repositories
{
    public interface IVoucherRepository
    {
        // ── CRUD ────────────────────────────────────────────────────────
        Task<Voucher?> GetByIdAsync(Guid id);
        Task<Voucher?> GetByCodeAndShopIdAsync(string code, Guid shopId);
        Task<List<Voucher>> GetByShopIdAsync(Guid shopId, string? statusFilter = null);
        Task AddAsync(Voucher voucher);
        void Update(Voucher voucher);
        Task DeleteAsync(Guid id);

        // ── Atomic Apply (chống race condition) ─────────────────────────
        /// <summary>
        /// Atomic update: UsedCount += 1, UsedBudget += discount.
        /// Chỉ update nếu UsedCount &lt; UsageLimit VÀ (MaxBudget IS NULL OR UsedBudget + discount &lt;= MaxBudget).
        /// Trả về true nếu update thành công, false nếu voucher đã hết / invalid.
        /// </summary>
        Task<bool> AtomicApplyAsync(Guid voucherId, decimal discountAmount);

        /// <summary>
        /// Hoàn ngược lại AtomicApply khi Order bị CANCEL hoặc REFUND.
        /// Giảm UsedCount và UsedBudget, xóa VoucherUsage record tương ứng.
        /// </summary>
        Task RollbackApplyAsync(Guid voucherId, decimal discountAmount, Guid orderId);

        // ── VoucherUsage ────────────────────────────────────────────────
        Task<int> GetUsageCountByBuyerAsync(Guid voucherId, Guid buyerId);
        Task AddUsageAsync(VoucherUsage usage);

        // ── State ────────────────────────────────────────────────────────
        Task<bool> CodeExistsForShopAsync(string code, Guid shopId, Guid? excludeId = null);
    }
}
