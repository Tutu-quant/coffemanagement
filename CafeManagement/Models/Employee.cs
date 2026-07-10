using System.ComponentModel.DataAnnotations;
namespace CafeManagement.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeID { get; set; }
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }
        [MaxLength(10)]
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        [MaxLength(20)]
        public string? Phone { get; set; }
        [MaxLength(100)]
        public string? Email { get; set; }
        [MaxLength(255)]
        public string? Address { get; set; }
        public decimal? Salary { get; set; }
        public DateTime? HireDate { get; set; }
        [MaxLength(20)]
        public string? Status { get; set; }
        // Quan hệ 1-1 với User
        public virtual User? User { get; set; }
    }
}