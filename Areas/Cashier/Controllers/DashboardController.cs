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
    [SessionAuthorize("Cashier,Admin")]  // Allow Cashier or Admin role
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
            var today = DateTime.Today;
            var model = new CashierDashboardViewModel();

            try
            {
                // Lấy tất cả bàn
                var tables = await _context.RestaurantTables
                    .Where(t => !t.IsDeleted)
                    .ToListAsync();

                // Lấy tất cả order hôm nay
                var todayOrders = await _context.Orders
                    .Where(o => o.OrderDate.Date == today && o.OrderStatus != "Cancelled")
                    .Include(o => o.Table)
                    .ToListAsync();

                // Lấy tất cả reservations hôm nay và sắp tới
                var todayReservations = await _context.Reservations
                    .Where(r => r.ReservationTime.Date >= today && r.ReservationStatus != "Cancelled")
                    .Include(r => r.Table)
                    .Include(r => r.Customer)
                    .OrderBy(r => r.ReservationTime)
                    .ToListAsync();

                // Tính doanh thu hôm nay
                var todayPayments = await _context.Payments
                    .Where(p => p.CreatedAt.Date == today && p.PaymentStatus == "Completed")
                    .ToListAsync();
                model.TodayRevenue = todayPayments.Sum(p => p.Amount);

                // Build dashboard tables
                foreach (var table in tables)
                {
                    var tableItem = new TableDashboardItemViewModel
                    {
                        TableID = table.TableID,
                        TableNumber = table.TableNumber,
                        Capacity = table.Capacity,
                        Location = table.Location
                    };

                    // Kiểm tra reservation cho bàn này
                    var reservation = todayReservations
                        .FirstOrDefault(r => r.TableID == table.TableID);

                    // Kiểm tra order cho bàn này
                    var order = todayOrders
                        .FirstOrDefault(o => o.TableID == table.TableID);

                    // Xác định trạng thái bàn
                    if (order != null)
                    {
                        if (order.OrderStatus == "Paid" || order.OrderStatus == "Completed")
                        {
                            tableItem.TableStatus = "Empty";
                        }
                        else if (order.OrderStatus == "PendingPayment")
                        {
                            tableItem.TableStatus = "PendingPayment";
                            tableItem.OrderID = order.OrderID;
                            tableItem.OrderStatus = order.OrderStatus;
                            tableItem.OrderTotalAmount = order.TotalAmount;
                            tableItem.OrderCreatedAt = order.CreatedAt;
                        }
                        else
                        {
                            tableItem.TableStatus = "Serving";
                            tableItem.OrderID = order.OrderID;
                            tableItem.OrderStatus = order.OrderStatus;
                            tableItem.OrderTotalAmount = order.TotalAmount;
                            tableItem.OrderCreatedAt = order.OrderDate;
                            // Lấy số lượng items trong order
                            var orderDetails = await _context.OrderDetails
                                .Where(od => od.OrderID == order.OrderID && !od.IsDeleted)
                                .ToListAsync();
                            tableItem.OrderItemCount = orderDetails.Count;
                        }
                    }
                    else if (reservation != null && reservation.ReservationTime > DateTime.Now)
                    {
                        tableItem.TableStatus = "Reserved";
                        tableItem.ReservationCustomerName = reservation.Customer?.CustomerName ?? "N/A";
                        tableItem.ReservationTime = reservation.ReservationTime;
                        tableItem.ReservationGuestCount = reservation.NumberOfGuests;
                    }
                    else
                    {
                        tableItem.TableStatus = "Empty";
                    }

                    model.Tables.Add(tableItem);
                }

                // Tính thống kê
                model.TotalTables = tables.Count;
                model.EmptyTables = model.Tables.Count(t => t.TableStatus == "Empty");
                model.ReservedTables = model.Tables.Count(t => t.TableStatus == "Reserved");
                model.ServingTables = model.Tables.Count(t => t.TableStatus == "Serving");
                model.PendingPaymentTables = model.Tables.Count(t => t.TableStatus == "PendingPayment");

                // Build upcoming reservations (next 5)
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

                // Build notifications
                model.Notifications = BuildNotifications(model, todayOrders, todayReservations);
            }
            catch (Exception ex)
            {
                // Log error if needed
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

            // Notification: Các bàn chờ thanh toán
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

            // Notification: Khách sắp tới (trong 15 phút)
            var soonReservations = todayReservations
                .Where(r => r.ReservationTime > now && r.ReservationTime <= now.AddMinutes(15))
                .ToList();

            foreach (var res in soonReservations)
            {
                var minutesLeft = (int)(res.ReservationTime - now).TotalMinutes;
                notifications.Add(new DashboardNotificationViewModel
                {
                    Title = "⏰ Khách Sắp Đến",
                    Message = $"Bàn {res.Table?.TableNumber} - {res.Customer?.CustomerName} ({res.NumberOfGuests} người) - Còn {minutesLeft} phút",
                    Type = "warning",
                    Icon = "fa-clock",
                    CreatedAt = now,
                    IsRead = false
                });
            }

            // Notification: Bàn quá giờ
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

            // Notification: Bàn trống (optional - only if none are empty)
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
