namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Reservation
    {
        public int ReservationID { get; set; }

        public int CustomerID { get; set; }

        public int TableID { get; set; }

        public DateTime ReservationDate { get; set; }

        public DateTime? CheckinTime { get; set; }

        public DateTime? CheckoutTime { get; set; }

        public int NumberOfGuests { get; set; }

        public string ReservationStatus { get; set; } = "Pending";

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual Customer? Customer { get; set; }
        public virtual RestaurantTable? Table { get; set; }
    }
}
