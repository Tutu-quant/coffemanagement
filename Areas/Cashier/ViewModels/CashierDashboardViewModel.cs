using System;
using System.Collections.Generic;

namespace Quản_lý_quán_cafe.Areas.Cashier.ViewModels
{
<<<<<<< HEAD
=======


>>>>>>> b4f1700646f7c1f88575a79e86ff337a8d546073
    public class CashierDashboardViewModel
    {
        // Thống kê bàn
        public int TotalTables { get; set; }
        public int EmptyTables { get; set; }
        public int ReservedTables { get; set; }
        public int ServingTables { get; set; }
        public int PendingPaymentTables { get; set; }

        // Doanh thu
        public decimal TodayRevenue { get; set; }
        public int TodayReservations { get; set; }

        // Danh sách chi tiết
        public List<TableDashboardItemViewModel> Tables { get; set; } = new();
        public List<UpcomingReservationViewModel> UpcomingReservations { get; set; } = new();
        public List<DashboardNotificationViewModel> Notifications { get; set; } = new();
    }

    public class TableDashboardItemViewModel
    {
        public int TableID { get; set; }
        public string TableNumber { get; set; }
        public int Capacity { get; set; }
        public string TableStatus { get; set; } // Empty, Reserved, Serving, PendingPayment
        public string? ReservationCustomerName { get; set; }
        public DateTime? ReservationTime { get; set; }
        public int? ReservationGuestCount { get; set; }
        public int? OrderID { get; set; }
        public decimal? OrderTotalAmount { get; set; }
        public string? OrderStatus { get; set; }
        public int? OrderItemCount { get; set; }
        public DateTime? OrderCreatedAt { get; set; }
        public string? Location { get; set; }

        // Computed properties
        public TimeSpan TimeUsed => OrderCreatedAt.HasValue ? DateTime.Now - OrderCreatedAt.Value : TimeSpan.Zero;
        public int MinutesUsed => (int)TimeUsed.TotalMinutes;
        public TimeSpan TimeUntilReservation => ReservationTime.HasValue ? ReservationTime.Value - DateTime.Now : TimeSpan.Zero;
        public int MinutesUntilReservation => Math.Max(0, (int)TimeUntilReservation.TotalMinutes);
        public bool IsOverdue => MinutesUsed > 90 && TableStatus == "Serving"; // Over 90 minutes
    }

    public class UpcomingReservationViewModel
    {
        public int ReservationID { get; set; }
        public DateTime ReservationTime { get; set; }
        public string TableNumber { get; set; }
        public string CustomerName { get; set; }
        public int GuestCount { get; set; }
        public string? Notes { get; set; }

        // Computed properties
        public TimeSpan TimeUntilArrival => ReservationTime - DateTime.Now;
        public int MinutesUntilArrival => Math.Max(0, (int)TimeUntilArrival.TotalMinutes);
        public string TimeDisplay => FormatTimeDisplay();

        private string FormatTimeDisplay()
        {
<<<<<<< HEAD
            int minutes = MinutesUntilArrival;
            if (minutes < 0) return "Đã quá giờ";
            if (minutes < 60) return $"Còn {minutes} phút";
            int hours = minutes / 60;
            int mins = minutes % 60;
            return $"Còn {hours}h {mins}p";
=======
            public int TableID { get; set; }
            public string TableName { get; set; } = string.Empty;
            public string Status { get; set; } = "Empty";
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
>>>>>>> b4f1700646f7c1f88575a79e86ff337a8d546073
        }
    }

    public class DashboardNotificationViewModel
    {
        public int NotificationID { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // "info", "warning", "danger", "success"
        public string Icon { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        // Computed properties
        public string TimeAgo => FormatTimeAgo();

        private string FormatTimeAgo()
        {
<<<<<<< HEAD
            TimeSpan timeSpan = DateTime.Now - CreatedAt;
            if (timeSpan.TotalSeconds < 60) return "Vừa xong";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h";
            return $"{(int)timeSpan.TotalDays}d";
=======
            public int OrderID { get; set; }
            public string OrderCode { get; set; } = string.Empty;
            public int TableID { get; set; }
            public string TableName { get; set; } = string.Empty;
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = "Pending";
            public DateTime CreatedAt { get; set; }
            public int ItemCount { get; set; }
>>>>>>> b4f1700646f7c1f88575a79e86ff337a8d546073
        }
    }
}
