using System.ComponentModel.DataAnnotations;

namespace Quản_lý_quán_cafe.Areas.Admin.ViewModels;

public class EmployeeFormViewModel
{
    public int EmployeeID { get; set; }
    [Required, StringLength(200), Display(Name = "Họ và tên")]
    public string FullName { get; set; } = string.Empty;
    [Required, Display(Name = "Giới tính")]
    public string Gender { get; set; } = "Khác";
    [DataType(DataType.Date), Display(Name = "Ngày sinh")]
    public DateTime? BirthDate { get; set; }
    [Phone, StringLength(20), Display(Name = "Số điện thoại")]
    public string? Phone { get; set; }
    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }
    [StringLength(500), Display(Name = "Địa chỉ")]
    public string? Address { get; set; }
    [DataType(DataType.Date), Display(Name = "Ngày vào làm")]
    public DateTime? HireDate { get; set; } = DateTime.Today;
    [Range(0, 1_000_000_000), Display(Name = "Lương")]
    public decimal? Salary { get; set; }
}
