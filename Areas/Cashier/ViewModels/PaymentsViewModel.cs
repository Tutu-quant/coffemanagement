using System;
using System.Collections.Generic;

namespace Quản_lý_quán_cafe.Areas.Cashier.ViewModels
{
    public class PaymentsViewModel
    {
        public decimal TotalPaidToday { get; set; }
        public decimal PendingAmount { get; set; }
        public int TransactionCountToday { get; set; }
        public List<TransactionItem> Transactions { get; set; } = new List<TransactionItem>();
    }

    public class TransactionItem
    {
        public string? OrderCode { get; set; }
        public string PaymentMethod { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime Time { get; set; }
        public string PaymentStatus { get; set; } = "";
    }
}
