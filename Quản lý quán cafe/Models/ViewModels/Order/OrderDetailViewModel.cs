namespace Quản_lý_quán_cafe.Models.ViewModels.Order
{
    /// <summary>
    /// ViewModel cho chi tiết đơn hàng
    /// </summary>
    public class OrderDetailViewModel
    {
        // Thông tin đơn hàng
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime? CompletedDate { get; set; }
        public string? Notes { get; set; }

        // Thông tin khách hàng
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        // Thông tin bàn
        public int? TableId { get; set; }
        public string? TableNumber { get; set; }
        public int? TableCapacity { get; set; }

        // Danh sách món
        public List<OrderItemViewModel> Items { get; set; } = new();

        // Thanh toán
        public int? PaymentId { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? PaidDate { get; set; }

        // Tính toán
        public decimal RemainingAmount => TotalAmount - PaidAmount;
        public int ItemCount => Items.Count;
        public string StatusBadgeClass { get; set; } = string.Empty;

        // Timeline
        public List<OrderTimelineEventViewModel> Timeline { get; set; } = new();
    }

    /// <summary>
    /// ViewModel cho sự kiện timeline
    /// </summary>
    public class OrderTimelineEventViewModel
    {
        public DateTime EventDate { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public string? EventDetails { get; set; }
    }
}
