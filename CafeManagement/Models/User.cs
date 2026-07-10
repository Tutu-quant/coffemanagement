using System.Data;
public class User
{
    public int UserID { get; set; }
    public int? EmployeeID { get; set; }
    public int? CustomerID { get; set; }
    public int RoleID { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime? CreatedAt { get; set; }
    public virtual Employee? Employee { get; set; }
    public virtual Customer? Customer { get; set; }
    public virtual Role Role { get; set; }
}