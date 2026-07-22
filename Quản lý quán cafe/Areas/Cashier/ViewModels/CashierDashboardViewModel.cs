namespace Quản_lý_quán_cafe.Areas.Cashier.ViewModels
{
    /// <summary>
    /// Dashboard Cashier - Tổng quan
    /// </summary>
    public class CashierDashboardViewModel
    {
        public int ActiveTablesCount { get; set; }
        public int TodayOrdersCount { get; set; }
        public int WaitingPaymentCount { get; set; }
        public decimal TodayRevenue { get; set; }

        public List<TableStatusDto> Tables { get; set; } = new();
        public List<OrderSummaryDto> RecentOrders { get; set; } = new();

        public class TableStatusDto
        {
            public int TableID { get; set; }
            public string TableName { get; set; } = string.Empty;
            public string Status { get; set; } = "Empty"; // Empty, Occupied, WaitingPayment, Reserved
            public int? OrderID { get; set; }
            public string? OrderCode { get; set; }
            public int? GuestCount { get; set; }
            public DateTime? StartTime { get; set; }
            public decimal? TotalAmount { get; set; }

            public string StatusColor => Status switch
            {
                "Occupied" => "#FFB84D",
                "WaitingPayment" => "#FF6B6B",
                "Reserved" => "#95E1D3",
                _ => "#E8E6E1"
            };

            public string StatusLabel => Status switch
            {
                "Occupied" => "Có khách",
                "WaitingPayment" => "Chờ thanh toán",
                "Reserved" => "Đã đặt",
                _ => "Trống"
            };
        }

        public class OrderSummaryDto
        {
            public int OrderID { get; set; }
            public string OrderCode { get; set; } = string.Empty;
            public int TableID { get; set; }
            public string TableName { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = "Pending"; // Pending, Completed, Paid
            public DateTime CreatedAt { get; set; }
            public int ItemCount { get; set; }
        }
    }
}
