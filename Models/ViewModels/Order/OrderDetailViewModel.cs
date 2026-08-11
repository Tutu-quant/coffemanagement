namespace Quản_lý_quán_cafe.Models.ViewModels.Order
{


    public class OrderDetailViewModel
    {

        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public DateTime? CompletedDate { get; set; }
        public string? Notes { get; set; }


        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }


        public int? TableId { get; set; }
        public string? TableNumber { get; set; }
        public int? TableCapacity { get; set; }


        public List<OrderItemViewModel> Items { get; set; } = new();


        public int? PaymentId { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime? PaidDate { get; set; }

        public decimal LoyaltySubtotalAmount { get; set; }
        public decimal PointDiscountAmount { get; set; }
        public decimal VoucherDiscountAmount { get; set; }
        public string LoyaltyDiscountMode { get; set; } = "None";
        public string? AppliedVoucherCode { get; set; }
        public int AvailableRewardPoints { get; set; }
        public int AppliedRewardPoints { get; set; }
        public int ProjectedEarnedPoints { get; set; }
        public bool CanApplyLoyaltyDiscount { get; set; }


        public decimal RemainingAmount => TotalAmount - PaidAmount;
        public decimal Subtotal => Items.Sum(item => item.TotalPrice);
        public decimal DiscountAmount => Math.Max(0, Subtotal - TotalAmount);
        public int ItemCount => Items.Count;
        public string StatusBadgeClass { get; set; } = string.Empty;


        public List<OrderTimelineEventViewModel> Timeline { get; set; } = new();
    }


    public class OrderTimelineEventViewModel
    {
        public DateTime EventDate { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public string? EventDetails { get; set; }
    }
}
