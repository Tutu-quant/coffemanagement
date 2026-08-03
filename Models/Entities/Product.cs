namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Product
    {
        public int ProductID { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int CategoryID { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual Category? Category { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
