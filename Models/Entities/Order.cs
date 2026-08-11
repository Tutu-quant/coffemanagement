using Quản_lý_quán_cafe.Models.Enums;

namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Order
    {
        public int OrderID { get; set; }

        public int? CustomerID { get; set; }

        public bool IsLoyaltyCustomerAssigned { get; set; }

        public int? EmployeeID { get; set; }

        public int? TableID { get; set; }

        public decimal SubtotalAmount { get; set; }

        public decimal VoucherDiscountAmount { get; set; }

        public decimal PointDiscountAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public int? VoucherID { get; set; }

        public string? VoucherCode { get; set; }

        public string OrderStatus { get; set; } = OrderStatusConstants.Pending;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedDate { get; set; }

        public int? PaymentID { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; }


        public virtual Employee? Employee { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual RestaurantTable? Table { get; set; }
        public virtual Payment? Payment { get; set; }
        public virtual Voucher? Voucher { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<OrderPointRedemption> PointRedemptions { get; set; } = new List<OrderPointRedemption>();
    }
}
