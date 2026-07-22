namespace CafeManagement.Models.Entities
{
    public class Review
    {
        public int Id { get; set; }
        public string CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Relationships
        public virtual Customer Customer { get; set; }
        public virtual Product Product { get; set; }
    }
}
