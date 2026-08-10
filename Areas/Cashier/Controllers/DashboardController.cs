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

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    [SessionAuthorize("Cashier,Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var todayUtcStart = DateTime.UtcNow.Date;
            var todayUtcEnd = todayUtcStart.AddDays(1);
            var model = new CashierDashboardViewModel();

            try
            {
                var tables = await _context.RestaurantTables
                    .Where(t => !t.IsDeleted)
                    .ToListAsync();

                var todayOrders = await _context.Orders
                    .Where(o => o.OrderDate >= todayUtcStart && o.OrderDate < todayUtcEnd && o.OrderStatus != "Cancelled")
                    .Include(o => o.Table)
                    .ToListAsync();

                List<Reservation> todayReservations;
                try
                {
                    // Try to query by ReservationTime (preferred)
                    todayReservations = await _context.Reservations
                        .Where(r => r.ReservationTime >= DateTime.Today && r.ReservationStatus != "Cancelled")
                        .Include(r => r.Table)
                        .Include(r => r.Customer)
                        .OrderBy(r => r.ReservationTime)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    // Fallback: some DBs may not have ReservationTime column (older schema). Use ReservationDate instead.
                    // Log exception to ModelState for visibility in dev
                    ModelState.AddModelError("", "Reservation time column unavailable in DB, falling back to ReservationDate. " + ex.Message);
                    todayReservations = await _context.Reservations
                        .Where(r => r.ReservationDate >= DateTime.Today && r.ReservationStatus != "Cancelled")
                        .Include(r => r.Table)
                        .Include(r => r.Customer)
                        .OrderBy(r => r.ReservationDate)
                        .ToListAsync();
                }

                var todayPayments = await _context.Payments
                    .Where(p => p.CreatedAt >= todayUtcStart && p.CreatedAt < todayUtcEnd && p.PaymentStatus == "Completed")
                    .ToListAsync();
                model.TodayRevenue = todayPayments.Sum(p => p.Amount);

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

                    var reservation = todayReservations.FirstOrDefault(r => r.TableID == table.TableID);
                    if (reservation != null)
                    {
                        // Normalize reservation time as UTC then convert to server local time
                        var resUtc = DateTime.SpecifyKind(reservation.ReservationTime, DateTimeKind.Utc);
                        var resLocal = resUtc.ToLocalTime();
                        if (resLocal > DateTime.Now)
                        {
                            tableItem.ReservationCustomerName = reservation.Customer?.CustomerName ?? "N/A";
                            tableItem.ReservationTime = resLocal;
                            tableItem.ReservationGuestCount = reservation.NumberOfGuests;
                        }
                    }

                    var order = await _context.Orders
                        .Where(o => o.TableID == table.TableID && !o.IsDeleted && o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled")
                        .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
                        .OrderByDescending(o => o.OrderDate)
                        .FirstOrDefaultAsync();

                    if (order != null)
                    {
                        tableItem.OrderID = order.OrderID;
                        tableItem.OrderStatus = order.OrderStatus;
                        tableItem.OrderTotalAmount = order.TotalAmount;
                        tableItem.OrderCreatedAt = order.CreatedAt != default(DateTime) ? order.CreatedAt : order.OrderDate;
                        tableItem.OrderItemCount = order.OrderDetails?.Count ?? 0;

                        if (order.OrderStatus == "PendingPayment")
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

                model.UpcomingReservations = todayReservations
                    .Where(r => r.ReservationTime > DateTime.Now)
                    .Take(5)
                    .Select(r => new UpcomingReservationViewModel
                    {
                        ReservationID = r.ReservationID,
                        ReservationTime = r.ReservationTime,
                        TableNumber = r.Table?.TableNumber ?? "N/A",
                        CustomerName = r.Customer?.CustomerName ?? "N/A",
                        GuestCount = r.NumberOfGuests,
                        Notes = r.Notes
                    })
                    .ToList();

                model.TodayReservations = todayReservations.Count;

                model.Notifications = BuildNotifications(model, todayOrders, todayReservations);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Lỗi tải dữ liệu: {ex.Message}");
            }

            return View(model);
        }

        private List<DashboardNotificationViewModel> BuildNotifications(
            CashierDashboardViewModel model,
            List<Order> todayOrders,
            List<Reservation> todayReservations)
        {
            var notifications = new List<DashboardNotificationViewModel>();
            var now = DateTime.Now;

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

            // Normalize reservation times to UTC/local consistently when building notifications
            var nowUtc = DateTime.UtcNow;
            var soonReservations = todayReservations
                .Select(r => new { Res = r, Utc = DateTime.SpecifyKind(r.ReservationTime, DateTimeKind.Utc) })
                .Where(x => x.Utc > nowUtc && x.Utc <= nowUtc.AddMinutes(15))
                .ToList();

            foreach (var x in soonReservations)
            {
                var minutesLeft = (int)(x.Utc - nowUtc).TotalMinutes;
                // Display minutes relative to server local time but computed from UTC consistency
                notifications.Add(new DashboardNotificationViewModel
                {
                    Title = "⏰ Khách Sắp Đến",
                    Message = $"Bàn {x.Res.Table?.TableNumber} - {x.Res.Customer?.CustomerName} ({x.Res.NumberOfGuests} người) - Còn {minutesLeft} phút",
                    Type = "warning",
                    Icon = "fa-clock",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            var overdueTablesList = model.Tables
                .Where(t => t.IsOverdue)
                .Take(3)
                .ToList();

            foreach (var table in overdueTablesList)
            {
                notifications.Add(new DashboardNotificationViewModel
                {
                    Title = "🔴 Bàn Quá Giờ",
                    Message = $"Bàn {table.TableNumber} - Đã sử dụng {table.MinutesUsed} phút",
                    Type = "danger",
                    Icon = "fa-exclamation-circle",
                    CreatedAt = now.AddMinutes(-table.MinutesUsed),
                    IsRead = false
                });
            }

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
