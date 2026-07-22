namespace Quản_lý_quán_cafe.Models.ViewModels.Order
{
    /// <summary>
    /// ViewModel cho món hàng trong đơn
    /// </summary>
    public class OrderItemViewModel
    {
        public int OrderDetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Size { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        public string? Notes { get; set; }
    }
}
