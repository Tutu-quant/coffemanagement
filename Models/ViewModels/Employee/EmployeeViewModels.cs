using System.ComponentModel.DataAnnotations;
using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Models.ViewModels.Employees
{

public class EmployeeCreateViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    public string Username { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Mật khẩu không khớp")]
    public string ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Tên nhân viên là bắt buộc")]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Chức vụ là bắt buộc")]
    [StringLength(50)]
    public string Position { get; set; }

    [StringLength(50)]
    public string Department { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; }

    [StringLength(200)]
    public string Address { get; set; }
}

public class EmployeeEditViewModel
{
    public int EmployeeID { get; set; }

    [Required(ErrorMessage = "Tên nhân viên là bắt buộc")]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; }

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Chức vụ là bắt buộc")]
    [StringLength(50)]
    public string Position { get; set; }

    [StringLength(50)]
    public string Department { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; }

    [StringLength(200)]
    public string Address { get; set; }

    public bool IsActive { get; set; } = true;
}

public class EmployeeDetailViewModel
{
    public int EmployeeID { get; set; }
    public int UserID { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string Position { get; set; }
    public string Department { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime? HireDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class EmployeeListViewModel
{
    public List<Employee> Employees { get; set; } = new();
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set;}
    public int InactiveEmployees { get; set; }
    public string SearchTerm { get; set; } = "";
    public string SortBy { get; set; } = "newest";
    public string Department { get; set; } = "";
    public List<string> Departments { get; set; } = new();
}
}
