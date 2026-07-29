using System.ComponentModel.DataAnnotations;

namespace Quản_lý_quán_cafe.Models.ViewModels.RestaurantTable
{
    public class RestaurantTableViewModel
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string TableStatus { get; set; } = "Available";
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RestaurantTableListViewModel
    {
        public List<RestaurantTableViewModel> Tables { get; set; } = new();
        public List<string> Locations { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? SelectedLocation { get; set; }
        public string? SelectedStatus { get; set; }
        public string? SortBy { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;

        // Dashboard statistics
        public int TotalTables { get; set; }
        public int AvailableTables { get; set; }
        public int OccupiedTables { get; set; }
        public int WaitingPaymentTables { get; set; }
        public int MaintenanceTables { get; set; }
    }

    public class RestaurantTableCreateViewModel
    {
        [Required(ErrorMessage = "Mã bàn không được để trống")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Mã bàn phải từ 1 đến 50 ký tự")]
        [Display(Name = "Mã bàn")]
        public string TableNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sức chứa không được để trống")]
        [Range(1, 1000, ErrorMessage = "Sức chứa phải lớn hơn 0 và nhỏ hơn 1000")]
        [Display(Name = "Sức chứa")]
        public int Capacity { get; set; }

        [StringLength(100, ErrorMessage = "Khu vực không được quá 100 ký tự")]
        [Display(Name = "Khu vực")]
        public string? Location { get; set; }
    }

    public class RestaurantTableEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã bàn không được để trống")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Mã bàn phải từ 1 đến 50 ký tự")]
        [Display(Name = "Mã bàn")]
        public string TableNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sức chứa không được để trống")]
        [Range(1, 1000, ErrorMessage = "Sức chứa phải lớn hơn 0 và nhỏ hơn 1000")]
        [Display(Name = "Sức chứa")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống")]
        [Display(Name = "Trạng thái")]
        public string TableStatus { get; set; } = "Available";

        [StringLength(100, ErrorMessage = "Khu vực không được quá 100 ký tự")]
        [Display(Name = "Khu vực")]
        public string? Location { get; set; }
    }

    public class RestaurantTableDetailViewModel
    {
        public int Id { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string TableStatus { get; set; } = "Available";
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
