namespace Quản_lý_quán_cafe.Models.Entities
{
    public class User
    {
        public int UserID { get; set; }

        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public int RoleID { get; set; }

        public int? EmployeeID { get; set; }

        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? LastLogin { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation Properties
        public virtual Role? Role { get; set; }
        public virtual Employee? Employee { get; set; }
    }
}
