namespace CafeManagement.Models.Entities
{
    public class Reservation
    {
        public int Id { get; set; }
        public string ReservationCode { get; set; }
        public string CustomerId { get; set; }
        public int TableId { get; set; }
        public DateTime ReservationTime { get; set; }
        public int NumberOfGuests { get; set; }
        public string Status { get; set; } = "Pending";
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relationships
        public virtual Customer Customer { get; set; }
        public virtual RestaurantTable Table { get; set; }
    }
}
