using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.Models
{
    // Đặt bàn / đặt ghế trước
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Bàn")]
        public int TableId { get; set; }
        public RestaurantTable? Table { get; set; }

        // Người dùng đã đăng nhập đặt bàn (có thể null nếu nhân viên tạo hộ khách vãng lai)
        public string? CustomerId { get; set; }
        public ApplicationUser? Customer { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khách")]
        [StringLength(100)]
        [Display(Name = "Tên khách hàng")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Thời gian đặt")]
        [DataType(DataType.DateTime)]
        public DateTime BookingTime { get; set; }

        [Required]
        [Range(1, 50, ErrorMessage = "Số khách phải từ 1-50")]
        [Display(Name = "Số lượng khách (ghế)")]
        public int NumberOfGuests { get; set; }

        [Display(Name = "Trạng thái")]
        public BookingStatus Status { get; set; } = BookingStatus.ChoXacNhan;

        [StringLength(300)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        // Tiền cọc đặt bàn/đặt ghế = số ghế x đơn giá cọc/ghế
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tiền cọc")]
        public decimal DepositAmount { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Nhân viên xác nhận đặt bàn
        public string? ConfirmedByStaffId { get; set; }
        public ApplicationUser? ConfirmedByStaff { get; set; }
    }
}
