using Microsoft.AspNetCore.Mvc.Rendering;

namespace Quản_lý_quán_cafe.Areas.Customer.ViewModels;

public class OrderMenuViewModel
{
    public int TableId { get; set; }
    public string? Notes { get; set; }
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
