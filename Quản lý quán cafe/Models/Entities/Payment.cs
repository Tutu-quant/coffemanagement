namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Payment
    {
        public int PaymentID { get; set; }

        public int OrderID { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "Cash";

        public string PaymentStatus { get; set; } = "Pending";

        public DateTime PaymentDate { get; set; }

        public string? TransactionCode { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual Order? Order { get; set; }
        public virtual ICollection<Promotion> PromotionUses { get; set; } = new List<Promotion>();
    }
}
