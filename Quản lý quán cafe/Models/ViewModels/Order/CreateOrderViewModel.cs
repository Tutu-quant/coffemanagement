using System.ComponentModel.DataAnnotations;

namespace Quản_lý_quán_cafe.Models.ViewModels.Order
{
    /// <summary>
    /// Dữ liệu nhận từ màn hình đặt món để tạo một đơn hàng mới.
    /// </summary>
    public class CreateOrderViewModel : IValidatableObject
    {
        /// <summary>
        /// Bàn phục vụ. Có thể để trống với đơn mang đi.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Bàn không hợp lệ")]
        public int? TableId { get; set; }

        /// <summary>
        /// Khách hàng đặt món, nếu đã có thông tin thành viên.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Khách hàng không hợp lệ")]
        public int? CustomerId { get; set; }

        [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
        public string? Notes { get; set; }

        [Required(ErrorMessage = "Đơn hàng phải có ít nhất một món")]
        [MinLength(1, ErrorMessage = "Đơn hàng phải có ít nhất một món")]
        public List<CreateOrderItemViewModel> Items { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Items.Count == 0)
            {
                yield return new ValidationResult(
                    "Đơn hàng phải có ít nhất một món",
                    new[] { nameof(Items) });
            }

            if (Items.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
            {
                yield return new ValidationResult(
                    "Mỗi món chỉ được xuất hiện một lần trong đơn hàng",
                    new[] { nameof(Items) });
            }
        }
    }

    /// <summary>
    /// Một món được chọn khi tạo đơn hàng.
    /// Giá được lấy từ cơ sở dữ liệu, không nhận từ client.
    /// </summary>
    public class CreateOrderItemViewModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "Món không hợp lệ")]
        public int ProductId { get; set; }

        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
        public int Quantity { get; set; } = 1;

        [StringLength(500, ErrorMessage = "Ghi chú món không được vượt quá 500 ký tự")]
        public string? Notes { get; set; }
    }
}
