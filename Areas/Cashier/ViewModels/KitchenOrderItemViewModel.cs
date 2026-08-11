namespace Quản_lý_quán_cafe.Areas.Cashier.ViewModels
{
    /// <summary>
    /// Order item view model for kitchen display
    /// </summary>
    public class KitchenOrderItemViewModel
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Notes { get; set; }
    }
}
