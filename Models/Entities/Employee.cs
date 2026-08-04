namespace Quản_lý_quán_cafe.Models.Entities
{
    public class Employee
    {
        public int EmployeeID { get; set; }

        public int UserID { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public string Department { get; set; } = "Chưa cập nhật";

        public string Gender { get; set; } = string.Empty;

        public DateTime? BirthDate { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public DateTime? HireDate { get; set; }

        public decimal? Salary { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

<<<<<<< HEAD
        // Navigation Properties
        public virtual User? User { get; set; }
=======

        public virtual ICollection<User> Users { get; set; } = new List<User>();
>>>>>>> b4f1700646f7c1f88575a79e86ff337a8d546073
    }
}
