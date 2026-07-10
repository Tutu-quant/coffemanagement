using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeManagement.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên món")]
        [StringLength(150)]
        [Display(Name = "Tên món")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0")]
        [Display(Name = "Giá bán (VNĐ)")]
        public decimal Price { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Ảnh (URL)")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Còn bán")]
        public bool IsAvailable { get; set; } = true;

        [Required]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
