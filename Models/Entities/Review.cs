namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Review
    {
        public int ReviewID { get; set; }

        public int ProductID { get; set; }

        public int CustomerID { get; set; }

        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime ReviewDate { get; set; }

        public bool IsApproved { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual Product? Product { get; set; }
        public virtual Customer? Customer { get; set; }
    }
}
