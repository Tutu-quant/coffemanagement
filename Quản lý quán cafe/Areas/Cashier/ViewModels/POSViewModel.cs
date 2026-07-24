namespace Quản_lý_quán_cafe.Areas.Cashier.ViewModels
{
    /// <summary>
    /// POS - Màn hình bán hàng (Thiết kế Figma 3 cột)
    /// </summary>
    public class POSViewModel
    {
        // Cột trái: Danh sách bàn mở
        public List<POSTableViewModel> OpenTables { get; set; } = new();
        public string SearchTableQuery { get; set; } = string.Empty;

        // Cột giữa: Thông tin bàn hiện tại
        public POSTableViewModel CurrentTable { get; set; } = new();
        public List<POSOrderItemViewModel> OrderItems { get; set; } = new();
        public POSCustomerViewModel Customer { get; set; } = new();
        public string Notes { get; set; } = string.Empty;

        // Cột phải: Tính toán thanh toán
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public int PointsToAdd { get; set; }

        // Phương thức thanh toán
        public string PaymentMethod { get; set; } = "cash"; // cash, qr
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }

        // Discount
        public string DiscountType { get; set; } = "None"; // None, Percent, Fixed
        public decimal DiscountValue { get; set; }
    }
}

/// <summary>
/// Table - Thông tin bàn (Cột trái & giữa)
/// </summary>
public class POSTableViewModel
{
    public int TableID { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty; // #2407
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "empty"; // empty, occupied, waitingpayment
    public string StatusBadge { get; set; } = string.Empty; // "THANH TOÁN"
}

/// <summary>
/// Order Item - Thông tin từng món (Cột giữa)
/// </summary>
public class POSOrderItemViewModel
{
    public int OrderDetailID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Size { get; set; } = "M"; // S, M, L
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Total => Price * Quantity;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Customer - Thông tin khách hàng (Cột giữa)
/// </summary>
public class POSCustomerViewModel
{
    public int? CustomerID { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RewardPoints { get; set; }
    public string MembershipTier { get; set; } = "Member"; // Member, Silver, Gold
}

