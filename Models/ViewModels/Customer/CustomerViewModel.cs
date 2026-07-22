using System.ComponentModel.DataAnnotations;

namespace Quản_lý_quán_cafe.Models.ViewModels.Customer
{
    public class CustomerViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int RewardPoints { get; set; }
        public decimal TotalSpent { get; set; }
        public string MembershipTier { get; set; } = "Member";
        public bool IsActive { get; set; } = true;
        public DateTime? LastVisit { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AvatarInitials { get; set; } = string.Empty;
    }

    public class CustomerListViewModel
    {
        public List<CustomerViewModel> Customers { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;

        public string? SearchTerm { get; set; }
        public string? SelectedMembershipTier { get; set; }
        public string? SortBy { get; set; }

        // Dashboard Statistics
        public int TotalCustomers { get; set; }
        public int VIPCustomers { get; set; }
        public long TotalPoints { get; set; }
        public int CustomersToday { get; set; }
    }

    public class CustomerCreateViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 200 ký tự")]
        [Display(Name = "Họ tên")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không quá 100 ký tự")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(300, ErrorMessage = "Địa chỉ không quá 300 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Điểm tích lũy không được âm")]
        [Display(Name = "Điểm tích lũy")]
        public int RewardPoints { get; set; } = 0;

        [Range(0, 999999999.99, ErrorMessage = "Tổng chi tiêu không được âm")]
        [Display(Name = "Tổng chi tiêu")]
        public decimal TotalSpent { get; set; } = 0;

        [Display(Name = "Hạng thành viên")]
        public string MembershipTier { get; set; } = "Member";

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
    }

    public class CustomerEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Họ tên từ 2 đến 200 ký tự")]
        [Display(Name = "Họ tên")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Định dạng số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không quá 100 ký tự")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(300, ErrorMessage = "Địa chỉ không quá 300 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Điểm tích lũy không được âm")]
        [Display(Name = "Điểm tích lũy")]
        public int RewardPoints { get; set; } = 0;

        [Range(0, 999999999.99, ErrorMessage = "Tổng chi tiêu không được âm")]
        [Display(Name = "Tổng chi tiêu")]
        public decimal TotalSpent { get; set; } = 0;

        [Display(Name = "Hạng thành viên")]
        public string MembershipTier { get; set; } = "Member";

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
    }

    public class CustomerDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public int RewardPoints { get; set; }
        public decimal TotalSpent { get; set; }
        public string MembershipTier { get; set; } = "Member";
        public bool IsActive { get; set; } = true;
        public DateTime? LastVisit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<OrderDto> RecentOrders { get; set; } = new();
        public string AvatarInitials { get; set; } = string.Empty;

        public class OrderDto
        {
            public int Id { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = string.Empty;
        }
    }

    public class CustomerStatisticsViewModel
    {
        public int TotalCustomers { get; set; }
        public int VIPCustomers { get; set; }
        public long TotalPoints { get; set; }
        public int CustomersToday { get; set; }
    }
}
