using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.Models
{
    public class Beverage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        [Display(Name = "Sản phẩm")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(150)]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0")]
        [Display(Name = "Giá (VNĐ)")]
        public decimal Price { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active"; // Active, Inactive

        [ForeignKey(nameof(CategoryId))]
        public Category? Category { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }
    }
}
