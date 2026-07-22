namespace CafeManagement.Models.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime PaymentDate { get; set; }
        public string TransactionId { get; set; }
        public string Notes { get; set; }

        // Relationships
        public virtual Order Order { get; set; }
    }
}
