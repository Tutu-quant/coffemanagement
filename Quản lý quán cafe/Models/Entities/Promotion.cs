namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Promotion
    {
        public int PromotionID { get; set; }

        public int? ProductID { get; set; }

        public int? PaymentID { get; set; }

        public string PromotionName { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal DiscountPercentage { get; set; }

        public decimal? DiscountAmount { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual Product? Product { get; set; }
        public virtual Payment? Payment { get; set; }
    }
}
