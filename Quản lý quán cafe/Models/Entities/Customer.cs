namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Customer
    {
        public int CustomerID { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public int RewardPoints { get; set; } = 0;

        public decimal TotalSpent { get; set; } = 0;

        public string MembershipTier { get; set; } = "Member"; // Member, Silver, Gold, Diamond, VIP

        public bool IsActive { get; set; } = true;

        public DateTime? LastVisit { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public virtual ICollection<PointHistory> PointHistories { get; set; } = new List<PointHistory>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
