namespace Quản_lý_quán_cafe.Models.Entities
{
    /// <summary>
    /// Append-only audit entry for a customer's point balance. Corrections are
    /// represented by another entry instead of changing a historical entry.
    /// </summary>
    public class PointHistory
    {
        public int PointHistoryID { get; set; }

        public int CustomerID { get; set; }

        /// <summary>Signed point delta: positive for grants/earnings, negative for redemption.</summary>
        public int Points { get; set; }

        public int BalanceAfter { get; set; }

        public string TransactionType { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int? OrderID { get; set; }

        public int? ActorUserID { get; set; }

        /// <summary>Caller-supplied key used to make grants, earnings and redemptions idempotent.</summary>
        public string? IdempotencyKey { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Kept for compatibility with databases made by the original reward feature.
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public virtual Customer? Customer { get; set; }
        public virtual Order? Order { get; set; }
        public virtual User? ActorUser { get; set; }
        public virtual OrderPointRedemption? PointRedemption { get; set; }
    }
}
