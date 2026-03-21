using System;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IOpenReturnUseCase
    {
        Task<Guid> ExecuteAsync(OpenReturnRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// GĐ5A Bước 1: Buyer mock mở Return Request.
    /// Validate: Order DELIVERED + trong return window + chưa có active return.
    /// </summary>
    public class OpenReturnUseCase : IOpenReturnUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderReturnRepository _returnRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OpenReturnUseCase(
            IOrderRepository orderRepository,
            IOrderReturnRepository returnRepository,
            IProductRepository productRepository,
            IPolicyRepository policyRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _returnRepository = returnRepository;
            _productRepository = productRepository;
            _policyRepository = policyRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Guid> ExecuteAsync(OpenReturnRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
                if (order == null)
                    throw new ArgumentException("Order not found.");

                // [Validation] Chỉ DELIVERED mới được return
                if (order.Status != "DELIVERED")
                    throw new InvalidOperationException($"Đơn hàng đang ở trạng thái '{order.Status}'. Chỉ đơn DELIVERED mới có thể yêu cầu trả hàng.");

                // [Validation] Phải trong return window
                if (order.ReturnDeadline.HasValue && DateTimeOffset.UtcNow > order.ReturnDeadline.Value)
                    throw new InvalidOperationException("Đã hết hạn yêu cầu trả hàng (return window expired).");

                // [Validation] Không cho phép tạo nhiều return cùng lúc
                var existingReturn = await _returnRepository.GetActiveByOrderIdAsync(order.Id, cancellationToken);
                if (existingReturn != null)
                    throw new InvalidOperationException("Đơn hàng đã có yêu cầu trả hàng đang xử lý.");

                // [FIX-L1] Xác định ReturnShippingPaidBy từ ReturnPolicy config
                // SNAD/Damaged → luôn SELLER (eBay MBG override, bất kể policy)
                // Buyer remorse → đọc từ ReturnPolicy.DomesticShippingPaidBy
                string returnShippingPaidBy = "BUYER"; // fallback
                if (request.Reason == "NOT_AS_DESCRIBED" || request.Reason == "DAMAGED")
                {
                    returnShippingPaidBy = "SELLER"; // eBay MBG: seller luôn chịu phí SNAD
                }
                else
                {
                    // Đọc ReturnPolicy config cho buyer remorse cases
                    var orderItem = order.Items?.FirstOrDefault();
                    if (orderItem != null)
                    {
                        var product = await _productRepository.GetByIdAsync(orderItem.ProductId, cancellationToken);
                        if (product?.ReturnPolicyId != null)
                        {
                            var retPolicy = await _policyRepository.GetReturnPolicyByIdAsync(
                                product.ReturnPolicyId.Value, cancellationToken);
                            if (retPolicy != null)
                            {
                                returnShippingPaidBy = retPolicy.DomesticShippingPaidBy; // "BUYER" or "SELLER"
                            }
                        }
                    }
                }

                // Tạo OrderReturn
                var returnEntity = new OrderReturn
                {
                    OrderId = order.Id,
                    BuyerId = order.BuyerId,
                    Reason = request.Reason,
                    BuyerMessage = request.BuyerMessage,
                    PhotoUrls = request.PhotoUrls,
                    ReturnShippingPaidBy = returnShippingPaidBy
                };
                returnEntity.InitializeDeadline(); // Auto-set: +3 ngày seller phải respond

                // [FIX-M4] AutoAcceptReturns: nếu seller config auto-accept → tự chấp nhận
                bool autoAccepted = false;
                var orderItemForPolicy = order.Items?.FirstOrDefault();
                if (orderItemForPolicy != null)
                {
                    var productForPolicy = await _productRepository.GetByIdAsync(orderItemForPolicy.ProductId, cancellationToken);
                    if (productForPolicy?.ReturnPolicyId != null)
                    {
                        var retPolicyForAuto = await _policyRepository.GetReturnPolicyByIdAsync(
                            productForPolicy.ReturnPolicyId.Value, cancellationToken);
                        if (retPolicyForAuto != null && retPolicyForAuto.AutoAcceptReturns)
                        {
                            returnEntity.AcceptReturn("ACCEPT_RETURN", "Auto-accepted by return policy");
                            autoAccepted = true;
                        }
                    }
                }

                // Order → RETURN_REQUESTED hoặc RETURN_IN_PROGRESS (nếu auto-accept)
                if (autoAccepted)
                    order.MarkAsReturnInProgress();
                else
                    order.MarkAsReturnRequested();

                await _returnRepository.AddAsync(returnEntity, cancellationToken);
                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return returnEntity.Id;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
