namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Order
    {
        public int OrderID { get; set; }

        public int? CustomerID { get; set; }

        public int? TableID { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = "Pending";

        public DateTime OrderDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        public int? PaymentID { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual Customer? Customer { get; set; }
        public virtual RestaurantTable? Table { get; set; }
        public virtual Payment? Payment { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
