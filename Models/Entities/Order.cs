namespace CafeManagement.Models.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public string OrderCode { get; set; }
        public string CustomerId { get; set; }
        public int? TableId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Relationships
        public virtual Customer Customer { get; set; }
        public virtual RestaurantTable Table { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual Payment Payment { get; set; }
    }
}
