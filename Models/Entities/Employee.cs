namespace CafeManagement.Models.Entities
{
    public class Employee
    {
        public string Id { get; set; }
        public string EmployeeCode { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public decimal Salary { get; set; }
        public string Phone { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }

        // Relationships
        public virtual ApplicationUser User { get; set; }
    }
}
