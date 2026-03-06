namespace EbayClone.Application.DTOs.Orders
{
    public class UpdateOrderStatusRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        
        // Thêm các fields tùy chọn nếu cần ở từng trạng thái
        // Ví dụ: Bắt buộc điền Carrier và TrackingCode khi đổi sang SHIPPED
        public string? ShippingCarrier { get; set; }
        public string? TrackingCode { get; set; }
    }
}
