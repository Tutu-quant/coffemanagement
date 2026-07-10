using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.Models
{
    // Hoá đơn / order gắn với 1 bàn (có thể phát sinh từ 1 booking hoặc khách vãng lai)
    public class Order
    {
        public int Id { get; set; }

        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        [Required]
        [Display(Name = "Bàn")]
        public int TableId { get; set; }
        public RestaurantTable? Table { get; set; }

        // Nhân viên lập order
        public string? StaffId { get; set; }
        public ApplicationUser? Staff { get; set; }

        [Display(Name = "Thời gian mở order")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Trạng thái")]
        public OrderStatus Status { get; set; } = OrderStatus.DangMo;

        // Giảm giá (nếu có), nhập trực tiếp bằng VNĐ
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giảm giá")]
        public decimal DiscountAmount { get; set; } = 0;

        [Display(Name = "Phương thức thanh toán")]
        public string? PaymentMethod { get; set; } // Tiền mặt / Chuyển khoản / Thẻ

        [Display(Name = "Thời gian thanh toán")]
        public DateTime? PaidAt { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        [NotMapped]
        public decimal SubTotal => OrderDetails?.Sum(d => d.SubTotal) ?? 0;

        // Tổng tiền = tiền món ăn - giảm giá - tiền cọc đã đặt trước (nếu có)
        [NotMapped]
        public decimal DepositApplied => Booking?.DepositAmount ?? 0;

        [NotMapped]
        public decimal TotalAmount => Math.Max(0, SubTotal - DiscountAmount - DepositApplied);
    }
}
