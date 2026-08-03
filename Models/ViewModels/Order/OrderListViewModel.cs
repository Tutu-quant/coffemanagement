namespace Quản_lý_quán_cafe.Models.ViewModels.Order
{
    /// <summary>
    /// ViewModel cho danh sách đơn hàng
    /// </summary>
    public class OrderListViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? TableNumber { get; set; }
        public string? EmployeeName { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string? PaymentStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public int ItemCount { get; set; }
        public string StatusBadgeClass { get; set; } = string.Empty; // For CSS styling
    }

    /// <summary>
    /// ViewModel cho danh sách đơn hàng (pagination)
    /// </summary>
    public class OrderListContainerViewModel
    {
        public List<OrderListViewModel> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
