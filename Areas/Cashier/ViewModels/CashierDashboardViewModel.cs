using System;
using System.Collections.Generic;
using Quản_lý_quán_cafe.Models;

namespace Quản_lý_quán_cafe.Areas.Cashier.ViewModels
{
    public class CashierDashboardViewModel
    {
        public int TotalTables { get; set; }
        public int EmptyTables { get; set; }
        public int ReservedTables { get; set; }
        public int ServingTables { get; set; }
        public int PendingPaymentTables { get; set; }
        public int ActiveTablesCount { get; set; }
        public int TodayOrdersCount { get; set; }
        public int WaitingPaymentCount { get; set; }

        public decimal TodayRevenue { get; set; }
        public int TodayReservations { get; set; }

        public List<TableDashboardItemViewModel> Tables { get; set; } = new();
        public List<UpcomingReservationViewModel> UpcomingReservations { get; set; } = new();
        public List<DashboardNotificationViewModel> Notifications { get; set; } = new();
        public List<KitchenOrderAlertViewModel> KitchenOrderAlerts { get; set; } = new();
    }

    public class TableDashboardItemViewModel
    {
        public int TableID { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string TableStatus { get; set; } = "Empty";
        public string? ReservationCustomerName { get; set; }
        public DateTime? ReservationTime { get; set; }
        public int? ReservationGuestCount { get; set; }
        public int? OrderID { get; set; }
        public decimal? OrderTotalAmount { get; set; }
        public string? OrderStatus { get; set; }
        public int? OrderItemCount { get; set; }
        public DateTime? OrderCreatedAt { get; set; }
        public string? Location { get; set; }

        public TimeSpan TimeUsed => OrderCreatedAt.HasValue ? BusinessClock.Now - OrderCreatedAt.Value : TimeSpan.Zero;
        public int MinutesUsed => (int)TimeUsed.TotalMinutes;
        public TimeSpan TimeUntilReservation => ReservationTime.HasValue ? ReservationTime.Value - BusinessClock.Now : TimeSpan.Zero;
        public int MinutesUntilReservation => (int)Math.Ceiling(TimeUntilReservation.TotalMinutes);
        public bool IsOverdue => MinutesUsed > 90 && TableStatus == "Serving"; 
    }

    public class UpcomingReservationViewModel
    {
        public int ReservationID { get; set; }
        public DateTime ReservationTime { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int GuestCount { get; set; }
        public string? Notes { get; set; }

        public TimeSpan TimeUntilArrival => ReservationTime - BusinessClock.Now;
        public int MinutesUntilArrival => (int)Math.Ceiling(TimeUntilArrival.TotalMinutes);
        public string TimeDisplay => FormatTimeDisplay();

        private string FormatTimeDisplay()
        {
            int minutes = MinutesUntilArrival;
            if (minutes < 0) return "Đã quá giờ";
            if (minutes < 60) return $"Còn {minutes} phút";
            int hours = minutes / 60;
            int mins = minutes % 60;
            return $"Còn {hours}h {mins}p";
        }
    }

    public class DashboardNotificationViewModel
    {
        public int NotificationID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; 
        public string Icon { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        public string TimeAgo => FormatTimeAgo();

        private string FormatTimeAgo()
        {
            TimeSpan timeSpan = BusinessClock.Now - CreatedAt;
            if (timeSpan.TotalSeconds < 60) return "Vừa xong";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h";
            return $"{(int)timeSpan.TotalDays}d";
        }
    }

    /// <summary>
    /// Kitchen order alert - hiển thị các đơn ở bếp có vấn đề (quá lâu, ưu tiên, gấp)
    /// Dùng chung threshold với Kitchen: WARNING 10 phút, URGENT 15 phút, OVERDUE 20 phút
    /// </summary>
    public class KitchenOrderAlertViewModel
    {
        public int OrderID { get; set; }
        public string TableNumber { get; set; } = string.Empty;
        public int ElapsedSeconds { get; set; }  // Elapsed time in seconds
        public string OrderStatus { get; set; } = string.Empty;  // "Pending" or "Preparing"
        public int ItemCount { get; set; }

        /// <summary>
        /// Priority level based on elapsed seconds:
        /// - "normal": < 10:00 (600s)
        /// - "warning": 10:00 - 14:59 (600s - 899s)
        /// - "urgent": 15:00 - 19:59 (900s - 1199s)
        /// - "overdue": >= 20:00 (1200s+)
        /// </summary>
        public string PriorityLevel { get; set; } = "normal";

        /// <summary>
        /// Display text: "", "ƯU TIÊN", "GẤP", "QUÁ LÂU"
        /// </summary>
        public string PriorityText { get; set; } = string.Empty;

        /// <summary>
        /// Badge CSS class: "alert-warning", "alert-urgent", "alert-overdue"
        /// </summary>
        public string BadgeClass { get; set; } = string.Empty;

        /// <summary>
        /// Icon: "fa-exclamation-triangle", "fa-fire", "fa-exclamation-circle"
        /// </summary>
        public string IconClass { get; set; } = string.Empty;

        /// <summary>
        /// Formatted elapsed time: "00:10:00", "00:15:30", etc.
        /// </summary>
        public string ElapsedTimeFormatted
        {
            get
            {
                var hours = ElapsedSeconds / 3600;
                var minutes = (ElapsedSeconds % 3600) / 60;
                var secs = ElapsedSeconds % 60;
                return $"{hours:D2}:{minutes:D2}:{secs:D2}";
            }
        }

        /// <summary>
        /// Short display: "10 phút", "15 phút", etc. (no seconds)
        /// </summary>
        public string ElapsedTimeShort
        {
            get
            {
                var minutes = ElapsedSeconds / 60;
                if (minutes > 0)
                    return $"{minutes} phút";
                else
                    return $"{ElapsedSeconds}s";
            }
        }

        /// <summary>
        /// Status display text
        /// </summary>
        public string StatusText => OrderStatus == "Pending" ? "Chờ làm" : "Đang pha";
    }
}
