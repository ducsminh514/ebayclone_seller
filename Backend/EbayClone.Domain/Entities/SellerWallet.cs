using System;

namespace EbayClone.Domain.Entities
{
    /// <summary>
    /// Seller Level constants — tránh typo khi dùng magic strings.
    /// </summary>
    public static class SellerLevels
    {
        public const string NEW = "NEW";
        public const string BELOW_STANDARD = "BELOW_STANDARD";
        public const string ABOVE_STANDARD = "ABOVE_STANDARD";
        public const string TOP_RATED = "TOP_RATED";

        public static readonly string[] All = { NEW, BELOW_STANDARD, ABOVE_STANDARD, TOP_RATED };

        public static bool IsValid(string level) => Array.IndexOf(All, level) >= 0;
    }

    public class SellerWallet
    {
        public Guid ShopId { get; set; } // Primary Key
        
        // Virtual Id to satisfy DTO mapping/logic that expects an 'Id'
        public Guid Id => ShopId;
        public string Currency { get; set; } = "VND";

        public decimal AvailableBalance { get; private set; } = 0;
        public decimal PendingBalance { get; private set; } = 0;
        // [Phase 3] Tiền bị hold do: return/dispute mở, seller mới, item đắt
        public decimal OnHoldBalance { get; private set; } = 0;
        public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
        
        // Navigation
        public Shop? Shop { get; set; }

        /// <summary>Tổng tất cả balances — dùng cho BalanceAfter snapshot.</summary>
        public decimal TotalBalance => PendingBalance + AvailableBalance + OnHoldBalance;

        public void AddPending(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive.");
            PendingBalance += amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// [Phase 3] Khi return/dispute mở → chuyển tiền từ Pending/Available sang OnHold.
        /// Giữ lại cho đến khi resolve.
        /// </summary>
        public void HoldForDispute(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive.");
            // Ưu tiên hold từ Pending trước (tiền escrow đơn đó)
            if (PendingBalance >= amount)
            {
                PendingBalance -= amount;
            }
            else
            {
                var fromPending = PendingBalance;
                PendingBalance = 0;
                AvailableBalance -= (amount - fromPending);
            }
            OnHoldBalance += amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// [Phase 3] Khi dispute resolve seller win → release hold về Available.
        /// ⚠️ Internal use only — prefer ProcessReleaseFromHold() để tránh dùng sai.
        /// </summary>
        public void ReleaseHold(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive.");
            if (OnHoldBalance < amount) throw new InvalidOperationException("Not enough on-hold balance.");
            OnHoldBalance -= amount;
            AvailableBalance += amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// [Dispute SELLER_WIN] Giải ngân từ OnHold → Available (sau phí sàn).
        /// Dùng khi tiền đang ở OnHold (do HoldForDispute trước đó).
        ///
        /// Safe: Nếu OnHold < totalDebit (mismatch data), chỉ giải ngân phần có sẵn.
        /// Returns: (actualDebit, actualCredit) — số tiền thực giải ngân được.
        /// </summary>
        public (decimal actualDebit, decimal actualCredit) ProcessReleaseFromHold(decimal totalDebit, decimal availableCredit)
        {
            if (totalDebit <= 0 || availableCredit <= 0) throw new ArgumentException("Amounts must be positive.");
            if (availableCredit > totalDebit) throw new ArgumentException("Credit cannot exceed debit.");

            decimal feeRate  = 1m - (availableCredit / totalDebit);
            decimal actualDebit = Math.Min(totalDebit, OnHoldBalance);

            if (actualDebit <= 0)
                return (0, 0); // OnHold cạn — không làm gì

            OnHoldBalance  -= actualDebit;
            decimal actualCredit = Math.Round(actualDebit * (1m - feeRate), 0);
            AvailableBalance += actualCredit;
            UpdatedAt = DateTimeOffset.UtcNow;

            return (actualDebit, actualCredit);
        }

        /// <summary>
        /// [Resilient] Giải ngân escrow: trừ Pending → cộng Available (sau phí sàn).
        ///
        /// SAFE DESIGN: Nếu Pending không đủ (do data drift: cancel + release chạy cùng lúc),
        /// chỉ giải ngân phần Pending có sẵn → KHÔNG BAO GIỜ trừ Available.
        /// Tránh ví âm do double-accounting.
        ///
        /// Returns: (actualDebit, actualCredit) — số thực tế giải ngân được.
        /// Caller dùng để log đúng số tiền thực.
        /// </summary>
        public (decimal actualDebit, decimal actualCredit) ProcessRelease(decimal totalDebit, decimal availableCredit)
        {
            if (totalDebit <= 0 || availableCredit <= 0) throw new ArgumentException("Amounts must be positive.");
            if (availableCredit > totalDebit) throw new ArgumentException("Credit cannot exceed debit (phí sàn không thể âm).");

            decimal feeRate = 1m - (availableCredit / totalDebit); // tỉ lệ phí (ví dụ 0.05)

            // Chỉ giải ngân phần Pending thực tế có (không trừ Available)
            decimal actualDebit = Math.Min(totalDebit, PendingBalance);

            if (actualDebit <= 0)
            {
                // Pending = 0 → không có gì để giải ngân (đơn này đã bị refund rồi)
                // Bỏ qua, không làm gì
                return (0, 0);
            }

            PendingBalance -= actualDebit;

            // Tính lại profit theo tỉ lệ phí (giữ đúng fee rate dù giải ngân partial)
            decimal actualCredit = Math.Round(actualDebit * (1m - feeRate), 0);

            AvailableBalance += actualCredit;
            UpdatedAt = DateTimeOffset.UtcNow;

            return (actualDebit, actualCredit);
        }


        /// <summary>
        /// [Legacy] Giải ngân escrow strict — THROWS nếu Pending thiếu.
        /// ⚠️ Nên dùng ProcessRelease() thay thế.
        /// </summary>
        [Obsolete("Dùng ProcessRelease() thay thế — resilient hơn")]
        public void ReleaseEscrow(decimal pendingDebit, decimal availableCredit)
        {
            if (pendingDebit <= 0 || availableCredit <= 0) throw new ArgumentException("Amounts must be positive.");
            if (PendingBalance < pendingDebit) throw new InvalidOperationException("Not enough pending balance.");
            if (availableCredit > pendingDebit) throw new ArgumentException("Available credit cannot exceed pending debit.");

            PendingBalance -= pendingDebit;
            AvailableBalance += availableCredit;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Withdraw(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive.");
            if (AvailableBalance < amount) throw new InvalidOperationException("Not enough available balance.");
            
            AvailableBalance -= amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
        
        /// <summary>
        /// [Legacy] Trừ Pending strict — THROWS nếu thiếu.
        /// ⚠️ Nên dùng ProcessRefund() thay thế.
        /// </summary>
        [Obsolete("Dùng ProcessRefund() thay thế — resilient hơn")]
        public void DeductPending(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive.");
            if (PendingBalance < amount) throw new InvalidOperationException("Not enough pending balance.");
            
            PendingBalance -= amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// [eBay Managed Payments] Refund with fallback:
        /// 1. Trừ OnHoldBalance trước (tiền đã hold cho đơn này)
        /// 2. Thiếu → trừ PendingBalance (tiền escrow đơn khác)
        /// 3. Thiếu → trừ AvailableBalance (tiền đã release)
        /// 4. Vẫn thiếu → cho phép AvailableBalance âm (ghi nợ, giống eBay charge bank)
        /// </summary>
        public (decimal fromOnHold, decimal fromPending, decimal fromAvailable) ProcessRefund(decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Amount must be positive.");
            
            decimal remaining = amount;
            decimal fromOnHold = 0, fromPending = 0, fromAvailable = 0;
            
            // 1. OnHold trước
            if (remaining > 0 && OnHoldBalance > 0)
            {
                fromOnHold = Math.Min(remaining, OnHoldBalance);
                OnHoldBalance -= fromOnHold;
                remaining -= fromOnHold;
            }
            // 2. Pending
            if (remaining > 0 && PendingBalance > 0)
            {
                fromPending = Math.Min(remaining, PendingBalance);
                PendingBalance -= fromPending;
                remaining -= fromPending;
            }
            // 3. Available (có thể đi âm = ghi nợ)
            if (remaining > 0)
            {
                fromAvailable = remaining;
                AvailableBalance -= fromAvailable;
            }
            
            UpdatedAt = DateTimeOffset.UtcNow;
            return (fromOnHold, fromPending, fromAvailable);
        }
    }
}
