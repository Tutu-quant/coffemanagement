namespace CafeManagement.Models.Entities
{
    public class RestaurantTable
    {
        public int Id { get; set; }
        public string TableCode { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; } = "Available";
        public string Location { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Relationships
        public virtual ICollection<Reservation> Reservations { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
    }
}
