using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IRespondReturnUseCase
    {
        Task ExecuteAsync(Guid shopId, Guid returnId, RespondReturnRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// GĐ5A Bước 2: Seller respond return request.
    /// 4 options: ACCEPT_RETURN, PARTIAL_REFUND, FULL_REFUND_KEEP_ITEM, DECLINE
    /// </summary>
    public class RespondReturnUseCase : IRespondReturnUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderReturnRepository _returnRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _txRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IShopRepository _shopRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RespondReturnUseCase(
            IOrderRepository orderRepository,
            IOrderReturnRepository returnRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository txRepository,
            IProductRepository productRepository,
            IPolicyRepository policyRepository,
            IShopRepository shopRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _returnRepository = returnRepository;
            _walletRepository = walletRepository;
            _txRepository = txRepository;
            _productRepository = productRepository;
            _policyRepository = policyRepository;
            _shopRepository = shopRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync(Guid shopId, Guid returnId, RespondReturnRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var returnEntity = await _returnRepository.GetByIdAsync(returnId, cancellationToken);
                if (returnEntity == null)
                    throw new ArgumentException("Return request not found.");

                var order = returnEntity.Order;
                if (order == null || order.ShopId != shopId)
                    throw new UnauthorizedAccessException("Bạn không có quyền xử lý return request này.");

                // [Concurrency] Check RowVersion
                if (returnEntity.RowVersion != null && request.RowVersion != null 
                    && !request.RowVersion.SequenceEqual(returnEntity.RowVersion))
                {
                    throw new InvalidOperationException("Return request đã được cập nhật bởi phiên khác. Vui lòng tải lại.");
                }

                switch (request.ResponseType)
                {
                    case "ACCEPT_RETURN":
                        // Seller chấp nhận → buyer sẽ gửi hàng lại
                        returnEntity.AcceptReturn("ACCEPT_RETURN", request.SellerMessage);
                        order.MarkAsReturnInProgress();
                        break;

                    case "FULL_REFUND_KEEP_ITEM":
                        // Seller hoàn full + buyer giữ hàng → hoàn ví ngay
                        returnEntity.AcceptReturn("FULL_REFUND_KEEP_ITEM", request.SellerMessage);

                        // [FIX-H4] eBay MBG rule:
                        // SNAD/Damaged → refund full (item + shipping)
                        // Buyer đổi ý (CHANGED_MIND, WRONG_ITEM) → refund item ONLY (shipping không refund)
                        var isSNAD = returnEntity.Reason == "NOT_AS_DESCRIBED" || returnEntity.Reason == "DAMAGED";
                        var refundAmount = isSNAD ? order.TotalAmount : order.ItemSubtotal;

                        returnEntity.MarkRefunded(refundAmount, 0);
                        order.MarkAsRefunded();

                        // Hoàn PendingBalance
                        await DeductPendingAsync(shopId, order,
                            isSNAD ? "Hoàn tiền full SNAD (item + shipping)" : "Hoàn tiền item only (buyer đổi ý, shipping không refund)",
                            cancellationToken, customAmount: refundAmount);

                        // [FIX-R1] Giảm TotalTransactions + TotalSalesAmount khi Full Refund + Keep
                        var shopFullRefund = await _shopRepository.GetByIdAsync(shopId, cancellationToken);
                        if (shopFullRefund != null)
                        {
                            shopFullRefund.TotalTransactions = Math.Max(0, shopFullRefund.TotalTransactions - 1);
                            shopFullRefund.TotalSalesAmount = Math.Max(0, shopFullRefund.TotalSalesAmount - order.ItemSubtotal);
                            _shopRepository.Update(shopFullRefund);
                        }
                        break;

                    case "PARTIAL_REFUND":
                        // [FIX-L6] Check ReturnPolicy.DomesticRefundMethod — MoneyBack-only không cho partial
                        var orderItemForRefundCheck = order.Items?.FirstOrDefault();
                        if (orderItemForRefundCheck != null)
                        {
                            var productForRefund = await _productRepository.GetByIdAsync(orderItemForRefundCheck.ProductId, cancellationToken);
                            if (productForRefund?.ReturnPolicyId != null)
                            {
                                var retPolicyRefund = await _policyRepository.GetReturnPolicyByIdAsync(
                                    productForRefund.ReturnPolicyId.Value, cancellationToken);
                                if (retPolicyRefund != null && retPolicyRefund.DomesticRefundMethod == "MoneyBack")
                                {
                                    throw new InvalidOperationException(
                                        "Return policy chỉ cho phép MoneyBack (hoàn tiền full). Không thể offer partial refund.");
                                }
                            }
                        }

                        // Seller offer partial → chờ buyer accept/reject (KHÔNG auto-refund)
                        if (!request.PartialRefundAmount.HasValue || request.PartialRefundAmount.Value <= 0)
                            throw new ArgumentException("Số tiền hoàn 1 phần phải > 0.");
                        if (request.PartialRefundAmount.Value > order.TotalAmount)
                            throw new ArgumentException("Số tiền hoàn không được vượt quá tổng đơn hàng.");

                        returnEntity.OfferPartialRefund(request.PartialRefundAmount.Value, request.SellerMessage);
                        // Order giữ nguyên status — chờ buyer respond
                        break;

                    case "DECLINE":
                        // [eBay Rule] SNAD/Damaged → KHÔNG được decline (Money Back Guarantee)
                        if (returnEntity.Reason == "NOT_AS_DESCRIBED" || returnEntity.Reason == "DAMAGED")
                            throw new InvalidOperationException("Không thể từ chối return SNAD/Damaged (Money Back Guarantee).");

                        // Chỉ decline nếu: No Returns policy + buyer đổi ý
                        returnEntity.DeclineReturn(request.SellerMessage);
                        // Order giữ RETURN_REQUESTED → buyer có thể escalate (GĐ5C)
                        break;

                    default:
                        throw new ArgumentException($"ResponseType không hợp lệ: '{request.ResponseType}'.");
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

        private async Task DeductPendingAsync(Guid shopId, Domain.Entities.Order order, string desc, CancellationToken ct, decimal? customAmount = null)
        {
            var wallet = await _walletRepository.GetByShopIdAsync(shopId, ct);
            if (wallet != null)
            {
                var amount = customAmount ?? order.TotalAmount;
                var (fromOnHold, fromPending, fromAvailable) = wallet.ProcessRefund(amount);
                _walletRepository.Update(wallet);

                var sources = new System.Collections.Generic.List<string>();
                if (fromOnHold > 0) sources.Add($"Hold: -{fromOnHold:N0}");
                if (fromPending > 0) sources.Add($"Pending: -{fromPending:N0}");
                if (fromAvailable > 0) sources.Add($"Available: -{fromAvailable:N0}");
                var balanceNote = sources.Count > 0 ? $" ({string.Join(", ", sources)})" : "";

                await _txRepository.AddAsync(new Domain.Entities.WalletTransaction
                {
                    ShopId = shopId,
                    Amount = -amount,
                    Type = "REFUND",
                    ReferenceId = order.Id,
                    ReferenceType = "ORDER_RETURN",
                    OrderNumber = order.OrderNumber,
                    Description = $"{desc} — Đơn #{order.OrderNumber}{balanceNote}",
                    BalanceAfter = wallet.PendingBalance + wallet.AvailableBalance + wallet.OnHoldBalance
                }, ct);
            }
        }
    }
}
