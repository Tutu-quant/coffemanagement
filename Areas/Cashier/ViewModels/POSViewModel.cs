namespace Quản_lý_quán_cafe.Areas.Cashier.ViewModels
{
    public class POSViewModel
    {
        public List<POSTableViewModel> OpenTables { get; set; } = new();
        public string SearchTableQuery { get; set; } = string.Empty;

        public POSTableViewModel CurrentTable { get; set; } = new();
        public List<POSOrderItemViewModel> OrderItems { get; set; } = new();
        public POSCustomerViewModel Customer { get; set; } = new();
        public string Notes { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public int PointsToAdd { get; set; }

        public string PaymentMethod { get; set; } = "cash";
        public decimal PaidAmount { get; set; }
        public decimal ChangeAmount { get; set; }

        public string DiscountType { get; set; } = "None";
        public decimal DiscountValue { get; set; }
    }

    public class POSTableViewModel
    {
        public int? OrderID { get; set; }
        public int TableID { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "empty";
        public string StatusBadge { get; set; } = string.Empty;
    }
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

    public class POSCustomerViewModel
    {
        public int? CustomerID { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int RewardPoints { get; set; }
        public string MembershipTier { get; set; } = "Member"; // Member, Silver, Gold
    }
}

