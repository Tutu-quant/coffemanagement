namespace Quản_lý_quán_cafe.Models.ViewModels.Order
{
    /// <summary>
    /// ViewModel cho tóm tắt đơn hàng (dùng cho Dashboard)
    /// </summary>
    public class OrderSummaryViewModel
    {
        public int PendingCount { get; set; }
        public int PreparingCount { get; set; }
        public int ReadyCount { get; set; }
        public int WaitingPaymentCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TodayOrders { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue => TodayOrders > 0 ? TodayRevenue / TodayOrders : 0;
    }
}
