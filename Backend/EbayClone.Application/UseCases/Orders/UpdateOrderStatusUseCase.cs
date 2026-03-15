using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EbayClone.Shared.DTOs.Orders;
using EbayClone.Application.Interfaces;
using EbayClone.Application.Interfaces.Repositories;
using EbayClone.Domain.Entities;

namespace EbayClone.Application.UseCases.Orders
{
    public interface IUpdateOrderStatusUseCase
    {
        Task<bool> ExecuteAsync(Guid shopId, Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
    }

    public class UpdateOrderStatusUseCase : IUpdateOrderStatusUseCase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ISellerWalletRepository _walletRepository;
        private readonly IWalletTransactionRepository _walletTransactionRepository;
        private readonly IProductRepository _productRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateOrderStatusUseCase(
            IOrderRepository orderRepository,
            ISellerWalletRepository walletRepository,
            IWalletTransactionRepository walletTransactionRepository,
            IProductRepository productRepository,
            IUnitOfWork unitOfWork)
        {
            _orderRepository = orderRepository;
            _walletRepository = walletRepository;
            _walletTransactionRepository = walletTransactionRepository;
            _productRepository = productRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> ExecuteAsync(Guid shopId, Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
                
                if (order == null)
                    throw new ArgumentException("Order not found.");
                    
                if (order.ShopId != shopId)
                    throw new UnauthorizedAccessException("You are not authorized to update this order."); // Blocking IDOR

                // --- OPTIMISTIC CONCURRENCY CHECK ---
                if (request.RowVersion == null || !request.RowVersion.SequenceEqual(order.RowVersion))
                {
                    throw new InvalidOperationException("Đơn hàng đã được cập nhật bởi một phiên làm việc khác. Vui lòng tải lại trang.");
                }

                switch (request.NewStatus)
                {
                    case "READY_TO_SHIP":
                        order.MarkAsPaid();
                        // 1. Trừ kho thật sự và xả khoá Reserved (ATOMIC)
                        foreach(var item in order.Items)
                        {
                            await _productRepository.DeductStockAtomicAsync(item.VariantId, item.Quantity, cancellationToken);
                        }

                        // 2. NGHIỆP VỤ 2024: Ghi nhận doanh thu treo (Escrow) ngay khi khách TRẢ TIỀN
                        var walletPaid = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                        if (walletPaid != null)
                        {
                            walletPaid.AddPending(order.TotalAmount);
                            _walletRepository.Update(walletPaid);

                            await _walletTransactionRepository.AddAsync(new WalletTransaction
                            {
                                ShopId = shopId,
                                Amount = order.TotalAmount,
                                Type = "ORDER_INCOME",
                                ReferenceId = order.Id,
                                ReferenceType = "ORDER",
                                Description = $"Tạm giữ {order.TotalAmount:N0} đ (Escrow) từ đơn hàng #{order.OrderNumber}",
                                BalanceAfter = walletPaid.PendingBalance
                            }, cancellationToken);
                        }
                        break;

                    case "PROCESSING":
                        order.MarkAsPrintedLabel();
                        break;

                    case "SHIPPED":
                        order.MarkAsShipped(request.ShippingCarrier ?? "Unknown", request.TrackingCode ?? "");
                        break;

                    case "DELIVERED":
                        order.MarkAsDelivered();
                        // Tính phí sàn 5% (Ghi nhận để chuẩn bị cho bước Giải ngân ReleaseFunds sau này)
                        order.PlatformFee = order.TotalAmount * 0.05m;
                        break;

                    case "CANCELLED":
                        // Nếu đã thanh toán (PAID), tiền đã vào PendingBalance -> Cần HOÀN TIỀN (Deduct Pending)
                        if (order.PaymentStatus == "PAID")
                        {
                            // 1. Hoàn trả ví Pending
                            var walletRefund = await _walletRepository.GetByShopIdAsync(shopId, cancellationToken);
                            if (walletRefund != null)
                            {
                                walletRefund.DeductPending(order.TotalAmount);
                                _walletRepository.Update(walletRefund);

                                await _walletTransactionRepository.AddAsync(new WalletTransaction
                                {
                                    ShopId = shopId,
                                    Amount = -order.TotalAmount,
                                    Type = "REFUND",
                                    ReferenceId = order.Id,
                                    ReferenceType = "ORDER",
                                    Description = $"Hoàn tiền tạm giữ {order.TotalAmount:N0} đ cho Buyer (Hủy đơn #{order.OrderNumber})",
                                    BalanceAfter = walletRefund.PendingBalance
                                }, cancellationToken);
                            }

                            // 2. Hoàn kho (Restock) vì PAID đã trừ kho thật
                            foreach(var item in order.Items)
                            {
                                await _productRepository.RestockVariantAsync(item.VariantId, item.Quantity, cancellationToken);
                            }
                        }
                        else if (order.PaymentStatus == "UNPAID")
                        {
                            // Nếu chưa thanh toán, chỉ xả khoá Reserved
                            foreach(var item in order.Items)
                            {
                                await _productRepository.ReleaseReservationAtomicAsync(item.VariantId, item.Quantity, cancellationToken);
                            }
                        }
                        
                        order.CancelOrder();
                        break;

                    default:
                        throw new ArgumentException($"Unsupported status update: {request.NewStatus}");
                }

                _orderRepository.Update(order);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
