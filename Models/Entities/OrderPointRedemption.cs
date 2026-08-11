namespace Quản_lý_quán_cafe.Models.Entities
{
    /// <summary>
    /// Immutable invoice snapshot describing how one account contributed points
    /// to an order. A unique order/customer pair prevents accidental double use.
    /// </summary>
    public class OrderPointRedemption
    {
        public int OrderPointRedemptionID { get; set; }

        public int OrderID { get; set; }

        public int CustomerID { get; set; }

        public int? PointHistoryID { get; set; }

        public int PointsUsed { get; set; }

        public decimal DiscountAmount { get; set; }

        public int Sequence { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Order? Order { get; set; }
        public virtual Customer? Customer { get; set; }
        public virtual PointHistory? PointHistory { get; set; }
    }
}
