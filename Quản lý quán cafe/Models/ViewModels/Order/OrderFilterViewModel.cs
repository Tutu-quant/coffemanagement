namespace Quản_lý_quán_cafe.Models.ViewModels.Order
{
    /// <summary>
    /// ViewModel cho lọc và tìm kiếm đơn hàng
    /// </summary>
    public class OrderFilterViewModel
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Kết quả lọc
        public List<OrderListViewModel> Results { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;

        // Dropdown lists
        public List<SelectListItem> StatusOptions { get; set; } = new();
        public List<SelectListItem> EmployeeOptions { get; set; } = new();
    }

    public class SelectListItem
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }
}
