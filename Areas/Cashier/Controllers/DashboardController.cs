using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Areas.Cashier.ViewModels;
using Quản_lý_quán_cafe.Models.Entities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quản_lý_quán_cafe.Models;
using Quản_lý_quán_cafe.Services;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    [SessionAuthorize("Cashier,Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;
        private readonly ReservationStatusService _reservationStatusService;

        public DashboardController(
            ApplicationDbContext context,
            ILogger<DashboardController> logger,
            ReservationStatusService reservationStatusService)
        {
            _context = context;
            _logger = logger;
            _reservationStatusService = reservationStatusService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var todayUtcStart = BusinessClock.StartOfTodayUtc;
            var todayUtcEnd = BusinessClock.StartOfTomorrowUtc;
            var model = new CashierDashboardViewModel();

            try
            {
                var tables = await _context.RestaurantTables
                    .Where(t => !t.IsDeleted)
                    .ToListAsync();

                var todayOrders = await _context.Orders
                    .Where(o => !o.IsDeleted && o.OrderDate >= todayUtcStart && o.OrderDate < todayUtcEnd && o.OrderStatus != "Cancelled")
                    .Include(o => o.Table)
                    .ToListAsync();

                var now = BusinessClock.Now;
                var reservationHoldCutoff = now.AddMinutes(ReservationPolicy.HoldBeforeMinutes);
                var localToday = BusinessClock.Today;
                var localTomorrow = localToday.AddDays(1);
                var todayReservations = await _context.Reservations
                    .Where(r => !r.IsDeleted && r.ReservationDate >= localToday.AddMinutes(-ReservationPolicy.DurationMinutes) &&
                                r.ReservationDate < localTomorrow && r.ReservationStatus != "Cancelled" &&
                                r.ReservationStatus != "Completed" && r.ReservationStatus != "CheckedIn")
                    .Include(r => r.Table)
                    .Include(r => r.Customer)
                    .OrderBy(r => r.ReservationDate)
                    .ToListAsync();
                var todayReservationCount = await _context.Reservations.CountAsync(r => !r.IsDeleted &&
                    r.ReservationDate >= localToday && r.ReservationDate < localTomorrow && r.ReservationStatus != "Cancelled");

                // Get upcoming reservations (next 5)
                var upcomingReservations = await _context.Reservations.AsNoTracking()
                    .Where(r => !r.IsDeleted && r.ReservationDate > now && r.ReservationStatus != "Cancelled" &&
                                r.ReservationStatus != "Completed" && r.ReservationStatus != "CheckedIn")
                    .Include(r => r.Table)
                    .Include(r => r.Customer)
                    .OrderBy(r => r.ReservationDate)
                    .Take(5)
                    .ToListAsync();

                // Get overdue reservations (past time but not yet auto-cancelled)
                var overdueReservations = await _reservationStatusService.GetOverdueReservationsAsync();

                var todayPayments = await _context.Payments
                    .Where(p => !p.IsDeleted && p.PaymentDate >= todayUtcStart && p.PaymentDate < todayUtcEnd && p.PaymentStatus == "Completed")
                    .ToListAsync();
                model.TodayRevenue = todayPayments.Sum(p => p.Amount);

                var activeOrdersByTable = (await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.TableID.HasValue && !o.IsDeleted && o.OrderStatus != "Completed" &&
                                o.OrderStatus != "Cancelled" && o.OrderDetails.Any(d => !d.IsDeleted))
                    .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync())
                    .GroupBy(o => o.TableID!.Value)
                    .ToDictionary(group => group.Key, group => group.First());

                foreach (var table in tables)
                {
                    var tableItem = new TableDashboardItemViewModel
                    {
                        TableID = table.TableID,
                        TableNumber = table.TableNumber,
                        Capacity = table.Capacity,
                        Location = table.Location
                    };

                    var status = (table.TableStatus ?? "Available").Trim();
                    tableItem.TableStatus = status switch
                    {
                        "Available" => "Empty",
                        "Reserved" => "Reserved",
                        "Occupied" => "Serving",
                        "WaitingPayment" => "PendingPayment",
                        "Maintenance" => "Maintenance",
                        _ => "Empty"
                    };

                    var reservation = todayReservations.FirstOrDefault(r =>
                        r.TableID == table.TableID &&
                        r.ReservationDate <= reservationHoldCutoff &&
                        r.ReservationDate.AddMinutes(ReservationPolicy.DurationMinutes) > now);
                    if (reservation != null)
                    {
                        tableItem.ReservationCustomerName = reservation.Customer?.CustomerName ?? "N/A";
                        tableItem.ReservationTime = reservation.ReservationDate;
                        tableItem.ReservationGuestCount = reservation.NumberOfGuests;
                        if (tableItem.TableStatus == "Empty") tableItem.TableStatus = "Reserved";
                    }

                    activeOrdersByTable.TryGetValue(table.TableID, out var order);

                    if (order != null)
                    {
                        tableItem.OrderID = order.OrderID;
                        tableItem.OrderStatus = order.OrderStatus;
                        tableItem.OrderTotalAmount = order.TotalAmount;
                        var createdAt = order.CreatedAt != default(DateTime) ? order.CreatedAt : order.OrderDate;
                        tableItem.OrderCreatedAt = BusinessClock.FromUtc(createdAt);
                        tableItem.OrderItemCount = order.OrderDetails?.Count ?? 0;

                        if (order.OrderStatus == "WaitingPayment")
                        {
                            tableItem.TableStatus = "PendingPayment";
                        }
                        else if (order.OrderStatus != "Completed" && order.OrderStatus != "Cancelled")
                        {
                            tableItem.TableStatus = "Serving";
                        }
                    }

                    model.Tables.Add(tableItem);
                }

                model.TotalTables = tables.Count;
                model.EmptyTables = model.Tables.Count(t => t.TableStatus == "Empty");
                model.ReservedTables = model.Tables.Count(t => t.TableStatus == "Reserved");
                model.ServingTables = model.Tables.Count(t => t.TableStatus == "Serving");
                model.PendingPaymentTables = model.Tables.Count(t => t.TableStatus == "PendingPayment");
                model.TodayOrdersCount = todayOrders.Count;
                model.ActiveTablesCount = model.ServingTables + model.PendingPaymentTables + model.ReservedTables;
                model.WaitingPaymentCount = model.PendingPaymentTables;

                model.UpcomingReservations = upcomingReservations
                    .Select(r => new UpcomingReservationViewModel
                    {
                        ReservationID = r.ReservationID,
                        ReservationTime = r.ReservationDate,
                        TableNumber = r.Table?.TableNumber ?? "N/A",
                        CustomerName = r.Customer?.CustomerName ?? "N/A",
                        GuestCount = r.NumberOfGuests,
                        Notes = r.Notes
                    })
                    .ToList();

                model.TodayReservations = todayReservationCount;

                model.Notifications = BuildNotifications(model, todayOrders, todayReservations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load cashier dashboard.");
                ModelState.AddModelError("", "Không thể tải dữ liệu tổng quan. Vui lòng thử lại.");
            }

            return View(model);
        }

        private List<DashboardNotificationViewModel> BuildNotifications(
            CashierDashboardViewModel model,
            List<Order> todayOrders,
            List<Reservation> todayReservations)
        {
            var notifications = new List<DashboardNotificationViewModel>();
            var now = BusinessClock.Now;

            // 1. Pending payment notifications
            if (model.PendingPaymentTables > 0)
            {
                var pendingTables = model.Tables
                    .Where(t => t.TableStatus == "PendingPayment")
                    .Take(3)
                    .ToList();

                foreach (var table in pendingTables)
                {
                    notifications.Add(new DashboardNotificationViewModel
                    {
                        Title = "💳 Chờ Thanh Toán",
                        Message = $"Bàn {table.TableNumber} - {table.OrderTotalAmount:N0}đ",
                        Type = "danger",
                        Icon = "fa-credit-card",
                        CreatedAt = now,
                        IsRead = false
                    });
                }
            }

            // 2. Upcoming reservations (next 15 minutes)
            var soonReservations = todayReservations
                .Where(r => r.ReservationDate > now && r.ReservationDate <= now.AddMinutes(15))
                .ToList();

            foreach (var reservation in soonReservations)
            {
                var minutesLeft = (int)(reservation.ReservationDate - now).TotalMinutes;
                notifications.Add(new DashboardNotificationViewModel
                {
                    Title = "⏰ Khách Sắp Đến",
                    Message = $"Bàn {reservation.Table?.TableNumber} - {reservation.Customer?.CustomerName} ({reservation.NumberOfGuests} người) - Còn {minutesLeft} phút",
                    Type = "warning",
                    Icon = "fa-clock",
                    CreatedAt = now,
                    IsRead = false
                });
            }

            // 3. Overdue reservations - showing reservation customers who are more than 0 minutes late
            var overdueReservations = todayReservations
                .Where(r => r.ReservationDate <= now && 
                           r.ReservationDate > now.AddMinutes(-ReservationPolicy.HoldBeforeMinutes) &&
                           (r.ReservationStatus == "Pending" || r.ReservationStatus == "Confirmed"))
                .ToList();

            foreach (var reservation in overdueReservations)
            {
                var minutesOverdue = (int)(now - reservation.ReservationDate).TotalMinutes;
                notifications.Add(new DashboardNotificationViewModel
                {
                    Title = "🔴 Bàn Quá Giờ",
                    Message = $"Bàn {reservation.Table?.TableNumber} - {reservation.Customer?.CustomerName} - Quá giờ {minutesOverdue} phút",
                    Type = "danger",
                    Icon = "fa-exclamation-circle",
                    CreatedAt = now.AddMinutes(-minutesOverdue),
                    IsRead = false
                });
            }

            // 4. Table overdue notifications (tables that have been serving for too long)
            var overdueTablesList = model.Tables
                .Where(t => t.IsOverdue)
                .Take(3)
                .ToList();

            foreach (var table in overdueTablesList)
            {
                notifications.Add(new DashboardNotificationViewModel
                {
                    Title = "⏱️ Bàn Sử Dụng Quá Lâu",
                    Message = $"Bàn {table.TableNumber} - Đã sử dụng {table.MinutesUsed} phút",
                    Type = "danger",
                    Icon = "fa-stopwatch",
                    CreatedAt = now.AddMinutes(-table.MinutesUsed),
                    IsRead = false
                });
            }

            // 5. No empty tables warning
            if (model.EmptyTables == 0)
            {
                notifications.Add(new DashboardNotificationViewModel
                {
                    Title = "📊 Hết Bàn Trống",
                    Message = "Tất cả bàn đang sử dụng. Không có bàn trống!",
                    Type = "info",
                    Icon = "fa-info-circle",
                    CreatedAt = now,
                    IsRead = false
                });
            }

            return notifications.OrderByDescending(n => n.CreatedAt).ToList();
        }
    }
}
