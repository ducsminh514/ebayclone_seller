using System;
using System.Threading;
using System.Threading.Tasks;

namespace EbayClone.Application.Interfaces
{
    /// <summary>
    /// [Performance Phase 2] Distributed Lock Service — chống duplicate processing khi chạy nhiều instances.
    /// 
    /// Use case chính: Background Services (FundRelease, Analytics, Reconciliation, EvaluateLevel, ListingActivator)
    /// chỉ cần 1 instance thực thi tại bất kỳ thời điểm nào.
    /// 
    /// Implementation: Redis SET NX EX pattern (lock with auto-expiry).
    /// </summary>
    public interface IDistributedLockService
    {
        /// <summary>
        /// Thử acquire lock. Nếu lock đã bị instance khác giữ → trả false → skip execution.
        /// expiry: thời gian lock tự expire (phòng trường hợp instance crash không release).
        /// </summary>
        Task<bool> TryAcquireLockAsync(string lockKey, TimeSpan expiry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Release lock sau khi xử lý xong. Chỉ release nếu lock thuộc về instance hiện tại.
        /// </summary>
        Task ReleaseLockAsync(string lockKey, CancellationToken cancellationToken = default);
    }
}
