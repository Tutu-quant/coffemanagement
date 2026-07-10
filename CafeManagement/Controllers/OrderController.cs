using CafeManagement.Data;
using CafeManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Controllers
{
    [Authorize(Policy = "StaffOrAdmin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Trang tổng quan: danh sách bàn + trạng thái + danh sách booking chờ xác nhận
        public async Task<IActionResult> Index()
        {
            ViewBag.Tables = await _context.RestaurantTables.OrderBy(t => t.TableName).ToListAsync();
            ViewBag.PendingBookings = await _context.Bookings
                .Include(b => b.Table)
                .Where(b => b.Status == BookingStatus.ChoXacNhan)
                .OrderBy(b => b.BookingTime)
                .ToListAsync();
            ViewBag.OpenOrders = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .Where(o => o.Status == OrderStatus.DangMo)
                .ToListAsync();
            return View();
        }

        // Xác nhận 1 booking -> đổi trạng thái bàn thành "Đã đặt"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            var booking = await _context.Bookings.Include(b => b.Table).FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            booking.Status = BookingStatus.DaXacNhan;
            booking.ConfirmedByStaffId = _userManager.GetUserId(User);
            if (booking.Table != null) booking.Table.Status = TableStatus.DaDat;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xác nhận đặt bàn.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            booking.Status = BookingStatus.DaHuy;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã từ chối/huỷ đặt bàn.";
            return RedirectToAction(nameof(Index));
        }

        // Tạo order mới cho 1 bàn (từ booking đã xác nhận, hoặc khách vãng lai)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartOrder(int tableId, int? bookingId)
        {
            var table = await _context.RestaurantTables.FindAsync(tableId);
            if (table == null) return NotFound();

            var existing = await _context.Orders
                .FirstOrDefaultAsync(o => o.TableId == tableId && o.Status == OrderStatus.DangMo);
            if (existing != null)
                return RedirectToAction(nameof(Details), new { id = existing.Id });

            var order = new Order
            {
                TableId = tableId,
                BookingId = bookingId,
                StaffId = _userManager.GetUserId(User),
                CreatedAt = DateTime.Now,
                Status = OrderStatus.DangMo
            };
            table.Status = TableStatus.DangPhucVu;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }

        // Chi tiết order: thêm/xoá món, xem tổng tiền
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Booking)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            ViewBag.Products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable)
                .OrderBy(p => p.Category!.Name).ThenBy(p => p.Name)
                .ToListAsync();
            return View(order);
        }

        // Thêm món vào order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int orderId, int productId, int quantity = 1)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.DangMo);
            if (order == null) return NotFound();

            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            if (quantity < 1) quantity = 1;

            var existingDetail = order.OrderDetails.FirstOrDefault(d => d.ProductId == productId);
            if (existingDetail != null)
            {
                existingDetail.Quantity += quantity;
            }
            else
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = orderId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                });
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // Cập nhật số lượng 1 dòng món
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem(int detailId, int quantity)
        {
            var detail = await _context.OrderDetails.FindAsync(detailId);
            if (detail == null) return NotFound();

            if (quantity <= 0)
                _context.OrderDetails.Remove(detail);
            else
                detail.Quantity = quantity;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = detail.OrderId });
        }

        // Xoá 1 món khỏi order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int detailId)
        {
            var detail = await _context.OrderDetails.FindAsync(detailId);
            if (detail == null) return NotFound();
            int orderId = detail.OrderId;
            _context.OrderDetails.Remove(detail);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // GET: xem trước hoá đơn thanh toán
        public async Task<IActionResult> Checkout(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Booking)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        // POST: xác nhận thanh toán -> tính tiền cuối cùng, đóng order, trả bàn về trạng thái Trống
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(int id, decimal discountAmount, string paymentMethod)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Booking)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            order.DiscountAmount = Math.Max(0, discountAmount);
            order.PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Tiền mặt" : paymentMethod;
            order.Status = OrderStatus.DaThanhToan;
            order.PaidAt = DateTime.Now;

            if (order.Table != null) order.Table.Status = TableStatus.Trong;
            if (order.Booking != null) order.Booking.Status = BookingStatus.HoanThanh;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Thanh toán thành công! Tổng tiền: {order.TotalAmount:N0} VNĐ.";
            return RedirectToAction(nameof(Invoice), new { id = order.Id });
        }

        // Hoá đơn để in
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Table)
                .Include(o => o.Booking)
                .Include(o => o.Staff)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        // Lịch sử hoá đơn đã thanh toán
        public async Task<IActionResult> History(DateTime? from, DateTime? to)
        {
            var query = _context.Orders
                .Include(o => o.Table)
                .Where(o => o.Status == OrderStatus.DaThanhToan)
                .AsQueryable();

            if (from.HasValue) query = query.Where(o => o.PaidAt >= from.Value);
            if (to.HasValue) query = query.Where(o => o.PaidAt <= to.Value);

            var orders = await query.OrderByDescending(o => o.PaidAt).ToListAsync();
            ViewBag.From = from;
            ViewBag.To = to;
            return View(orders);
        }
    }
}
