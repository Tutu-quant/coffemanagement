namespace CafeManagement.Models.Entities
{
    public class Customer
    {
        public string Id { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public int LoyaltyPoints { get; set; }
        public DateTime MembershipDate { get; set; } = DateTime.UtcNow;
        public bool IsVIP { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Relationships
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Reservation> Reservations { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<PointHistory> PointHistories { get; set; }
    }
}
