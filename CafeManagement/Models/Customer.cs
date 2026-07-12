using System.ComponentModel.DataAnnotations;
namespace CafeManagement.Models
{
    public class Customer
    {
        [Key]
        public int CustomerID { get; set; }
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }
        [MaxLength(20)]
        public string? Phone { get; set; }
        [MaxLength(100)]
        public string? Email { get; set; }
        public DateTime? CreatedAt { get; set; }
        public virtual User? User { get; set; }
    }
}