using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;
using EbayClone.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EbayClone.Infrastructure.Repositories
{
    public class VoucherRepository : IVoucherRepository
    {
        private readonly EbayDbContext _context;

        public VoucherRepository(EbayDbContext context)
        {
            _context = context;
        }

        // ── CRUD ─────────────────────────────────────────────────────────

        public async Task<Voucher?> GetByIdAsync(Guid id)
            => await _context.Vouchers.Include(v => v.Shop).FirstOrDefaultAsync(v => v.Id == id);

        public async Task<Voucher?> GetByCodeAndShopIdAsync(string code, Guid shopId)
            => await _context.Vouchers
                .FirstOrDefaultAsync(v => v.Code == code && v.ShopId == shopId);

        public async Task<List<Voucher>> GetByShopIdAsync(Guid shopId, string? statusFilter = null)
        {
            var query = _context.Vouchers.Where(v => v.ShopId == shopId);
            if (!string.IsNullOrWhiteSpace(statusFilter))
                query = query.Where(v => v.Status == statusFilter);
            return await query.OrderByDescending(v => v.ValidFrom).ToListAsync();
        }

        public async Task AddAsync(Voucher voucher)
            => await _context.Vouchers.AddAsync(voucher);

        public void Update(Voucher voucher)
            => _context.Vouchers.Update(voucher);

        public async Task DeleteAsync(Guid id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher != null)
                _context.Vouchers.Remove(voucher);
        }

        // ── Atomic Apply ─────────────────────────────────────────────────
        /// <summary>
        /// ATOMIC SQL UPDATE — chống race condition khi nhiều buyer cùng dùng 1 voucher.
        /// Chỉ update khi: UsedCount &lt; UsageLimit AND (MaxBudget IS NULL OR UsedBudget + discount &lt;= MaxBudget) AND Status = 'ACTIVE'.
        /// Return: true nếu update thành công, false nếu voucher đã hết / condition không thỏa.
        /// </summary>
        public async Task<bool> AtomicApplyAsync(Guid voucherId, decimal discountAmount)
        {
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE Vouchers
                  SET UsedCount = UsedCount + 1,
                      UsedBudget = UsedBudget + {0}
                  WHERE Id = {1}
                    AND Status = 'ACTIVE'
                    AND (UsageLimit = 0 OR UsedCount < UsageLimit)
                    AND (MaxBudget IS NULL OR UsedBudget + {0} <= MaxBudget)",
                discountAmount, voucherId);

            return rowsAffected > 0;
        }

        /// <summary>
        /// Hoàn ngược lại AtomicApply khi Order bị CANCEL hoặc REFUND.
        /// - Giảm UsedCount (không để âm)
        /// - Giảm UsedBudget (không để âm)
        /// - Xóa VoucherUsage record tương ứng với orderId
        /// </summary>
        public async Task RollbackApplyAsync(Guid voucherId, decimal discountAmount, Guid orderId)
        {
            // Atomic SQL: giảm counter, CASE WHEN để tránh giá trị âm
            await _context.Database.ExecuteSqlRawAsync(
                @"UPDATE Vouchers
                  SET UsedCount  = CASE WHEN UsedCount  > 0    THEN UsedCount  - 1    ELSE 0 END,
                      UsedBudget = CASE WHEN UsedBudget >= {0} THEN UsedBudget - {0}  ELSE 0 END
                  WHERE Id = {1}",
                discountAmount, voucherId);

            // Xóa VoucherUsage record (nếu tồn tại)
            var usage = await _context.VoucherUsages
                .FirstOrDefaultAsync(u => u.VoucherId == voucherId && u.OrderId == orderId);
            if (usage != null)
                _context.VoucherUsages.Remove(usage);
            // SaveChanges sẽ được gọi bởi UnitOfWork ở UseCase — không gọi ở đây
        }

        // ── VoucherUsage ─────────────────────────────────────────────────

        public async Task<int> GetUsageCountByBuyerAsync(Guid voucherId, Guid buyerId)
            => await _context.VoucherUsages
                .CountAsync(u => u.VoucherId == voucherId && u.BuyerId == buyerId);

        public async Task AddUsageAsync(VoucherUsage usage)
            => await _context.VoucherUsages.AddAsync(usage);

        // ── State ─────────────────────────────────────────────────────────

        public async Task<bool> CodeExistsForShopAsync(string code, Guid shopId, Guid? excludeId = null)
            => await _context.Vouchers.AnyAsync(v =>
                v.Code == code &&
                v.ShopId == shopId &&
                (excludeId == null || v.Id != excludeId));
    }
}
