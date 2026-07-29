namespace Quản_lý_quán_cafe.Models.Entities
{
    public class PointHistory
    {
        public int PointHistoryID { get; set; }

        public int CustomerID { get; set; }

        public int Points { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? OrderID { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual Customer? Customer { get; set; }
        public virtual Order? Order { get; set; }
    }
}
