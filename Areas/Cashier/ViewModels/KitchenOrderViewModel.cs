namespace Quản_lý_quán_cafe.Areas.Cashier.ViewModels
{
    /// <summary>
    /// Order view model for kitchen display
    /// </summary>
    public class KitchenOrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode => $"#{OrderId}";
        public int? TableId { get; set; }
        public string? TableNumber { get; set; }
        public int? TableCapacity { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string? OrderNotes { get; set; }
        public List<KitchenOrderItemViewModel> Items { get; set; } = new();

        /// <summary>
        /// Time elapsed since order was created (in minutes)
        /// </summary>
        public int ElapsedMinutes => (int)(DateTime.UtcNow - OrderDate).TotalMinutes;
    }
}
