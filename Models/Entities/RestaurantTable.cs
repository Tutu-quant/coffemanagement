namespace Quản_lý_quán_cafe.Models.Entities
{
    public class RestaurantTable
    {
        public int TableID { get; set; }

        public string TableNumber { get; set; } = string.Empty;

        public int Capacity { get; set; }

        public string TableStatus { get; set; } = "Available";

        public string? Location { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
