using System.ComponentModel.DataAnnotations;

namespace CafeManagement.Models
{
    // Đại diện cho 1 bàn trong quán cafe
    public class RestaurantTable
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên/số bàn")]
        [StringLength(50)]
        [Display(Name = "Tên bàn")]
        public string TableName { get; set; } = string.Empty;

        [Required]
        [Range(1, 50, ErrorMessage = "Số ghế phải từ 1-50")]
        [Display(Name = "Số ghế")]
        public int Capacity { get; set; }

        [StringLength(100)]
        [Display(Name = "Khu vực")]
        public string? Location { get; set; }

        [Display(Name = "Trạng thái")]
        public TableStatus Status { get; set; } = TableStatus.Trong;

        [StringLength(300)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
