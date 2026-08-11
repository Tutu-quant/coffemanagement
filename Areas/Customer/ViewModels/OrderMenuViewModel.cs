using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Quản_lý_quán_cafe.Areas.Customer.ViewModels;

public class OrderMenuViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn bàn phục vụ.")]
    public int TableId { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú chung không được vượt quá 1000 ký tự.")]
    public string? Notes { get; set; }

    [Required]
    [StringLength(200000, ErrorMessage = "Giỏ hàng quá lớn.")]
    public string CartJson { get; set; } = "[]";
    public List<SelectListItem> Tables { get; set; } = [];
    public List<MenuCategoryViewModel> Categories { get; set; } = [];
}

public class MenuCategoryViewModel
{
    public string Name { get; set; } = string.Empty;
    public List<MenuProductViewModel> Products { get; set; } = [];
}

public class MenuProductViewModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int AvailableQuantity { get; set; }
}

public record CartItemInput(int ProductId, int Quantity, string? Notes);
