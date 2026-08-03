namespace Quản_lý_quán_cafe.Areas.Admin.ViewModels;

public class DashboardViewModel
{
    public decimal TodayRevenue { get; set; }
    public decimal MonthRevenue { get; set; }
    public int TodayOrders { get; set; }
    public int ActiveTables { get; set; }
    public int TotalCustomers { get; set; }
    public int LowStockProducts { get; set; }
    public int PendingReservations { get; set; }
    public List<RevenuePoint> RevenueLast7Days { get; set; } = [];
    public List<BestSellerItem> BestSellers { get; set; } = [];
    public List<RecentOrderItem> RecentOrders { get; set; } = [];
    public List<LowStockItem> LowStockItems { get; set; } = [];
    public PaymentAccountViewModel PaymentAccount { get; set; } = new();

    public decimal RevenueGrowthPercent { get; set; }
    public decimal MaxDailyRevenue => RevenueLast7Days.Count == 0
        ? 0
        : RevenueLast7Days.Max(x => x.Revenue);
}

public class PaymentAccountViewModel
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập số điện thoại MoMo")]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^0\d{9}$", ErrorMessage = "Số MoMo phải gồm 10 chữ số và bắt đầu bằng 0")]
    [System.ComponentModel.DataAnnotations.Display(Name = "Số tài khoản MoMo")]
    public string AccountNumber { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
    [System.ComponentModel.DataAnnotations.StringLength(100)]
    [System.ComponentModel.DataAnnotations.Display(Name = "Tên người nhận")]
    public string AccountName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Cho phép thanh toán MoMo")]
    public bool IsActive { get; set; } = true;
}

public record RevenuePoint(DateTime Date, decimal Revenue, int OrderCount);
public record BestSellerItem(int ProductId, string ProductName, int Quantity, decimal Revenue);
public record RecentOrderItem(
    int OrderId,
    string TableName,
    string CustomerName,
    decimal TotalAmount,
    string Status,
    DateTime OrderDate);
public record LowStockItem(int ProductId, string ProductName, int Quantity);
