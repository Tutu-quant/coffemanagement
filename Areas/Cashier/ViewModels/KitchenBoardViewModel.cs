namespace Quản_lý_quán_cafe.Areas.Cashier.ViewModels
{
    /// <summary>
    /// Kitchen board view model containing order counts and order list
    /// </summary>
    public class KitchenBoardViewModel
    {
        public int PendingCount { get; set; }
        public int PreparingCount { get; set; }
        public int ReadyCount { get; set; }
        public List<KitchenOrderViewModel> Orders { get; set; } = new();
    }
}
