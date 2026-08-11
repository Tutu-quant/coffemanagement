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
}
