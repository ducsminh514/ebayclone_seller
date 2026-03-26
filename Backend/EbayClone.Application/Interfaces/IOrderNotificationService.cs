namespace EbayClone.Application.Interfaces
{
    /// <summary>
    /// Abstraction cho real-time order notifications.
    /// Application layer chỉ biết interface này, không biết SignalR.
    /// </summary>
    public interface IOrderNotificationService
    {
        /// <summary>Push thông báo đơn hàng mới cho seller (sau DB commit).</summary>
        Task NotifyNewOrderAsync(Guid shopId, Guid orderId, string orderNumber, decimal totalAmount);

        /// <summary>Push thông báo trạng thái đơn thay đổi.</summary>
        Task NotifyOrderStatusChangedAsync(Guid shopId, Guid orderId, string orderNumber, string oldStatus, string newStatus);

        /// <summary>Push thông báo buyer yêu cầu trả hàng.</summary>
        Task NotifyReturnRequestedAsync(Guid shopId, Guid orderId, string orderNumber);

        /// <summary>Push thông báo buyer mở dispute.</summary>
        Task NotifyDisputeOpenedAsync(Guid shopId, Guid orderId, string orderNumber, string disputeType);
    }
}
