namespace CafeManagement.Models.Entities
{
    public class PointHistory
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public int Points { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationships
        public virtual Customer Customer { get; set; }
    }
}
