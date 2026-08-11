namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Voucher
    {
        public const string PercentDiscount = "Percent";
        public const string FixedDiscount = "Fixed";

        public int VoucherID { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string DiscountType { get; set; } = FixedDiscount;

        public decimal DiscountValue { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
