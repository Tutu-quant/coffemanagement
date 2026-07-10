using CafeManagement.Data;
using CafeManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagement.Controllers
{
    [Authorize] // Bắt buộc đăng nhập mới được đặt bàn
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public BookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        private decimal DepositPerSeat => _configuration.GetValue<decimal?>("AppSettings:DepositPerSeat") ?? 20000m;

        // GET: Booking/Create - form đặt bàn
        public async Task<IActionResult> Create()
        {
            ViewBag.Tables = await _context.RestaurantTables
                .Where(t => t.Status != TableStatus.BaoTri)
                .OrderBy(t => t.TableName)
                .ToListAsync();
            ViewBag.DepositPerSeat = DepositPerSeat;

            var user = await _userManager.GetUserAsync(User);
            var model = new Booking
            {
                BookingTime = DateTime.Now.AddHours(1),
                CustomerName = user?.FullName ?? "",
                PhoneNumber = user?.PhoneNumber ?? ""
            };
            return View(model);
        }

        // POST: Booking/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking model)
        {
            ModelState.Remove(nameof(Booking.CustomerId));
            ModelState.Remove(nameof(Booking.Status));
            ModelState.Remove(nameof(Booking.DepositAmount));

            var table = await _context.RestaurantTables.FindAsync(model.TableId);
            if (table == null)
            {
                ModelState.AddModelError(string.Empty, "Bàn không tồn tại.");
            }
            else if (model.NumberOfGuests > table.Capacity)
            {
                ModelState.AddModelError(nameof(model.NumberOfGuests),
                    $"Bàn {table.TableName} chỉ tối đa {table.Capacity} ghế.");
            }

            if (model.BookingTime < DateTime.Now)
            {
                ModelState.AddModelError(nameof(model.BookingTime), "Thời gian đặt phải ở tương lai.");
            }

            // Kiểm tra trùng giờ (khoảng đệm 2 tiếng cho mỗi lượt đặt)
            if (table != null)
            {
                var windowStart = model.BookingTime.AddHours(-2);
                var windowEnd = model.BookingTime.AddHours(2);
                bool conflict = await _context.Bookings.AnyAsync(b =>
                    b.TableId == model.TableId &&
                    b.Status != BookingStatus.DaHuy &&
                    b.Status != BookingStatus.HoanThanh &&
                    b.BookingTime > windowStart && b.BookingTime < windowEnd);

                if (conflict)
                    ModelState.AddModelError(string.Empty, "Bàn đã có người đặt gần khung giờ này, vui lòng chọn bàn hoặc giờ khác.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Tables = await _context.RestaurantTables
                    .Where(t => t.Status != TableStatus.BaoTri).OrderBy(t => t.TableName).ToListAsync();
                ViewBag.DepositPerSeat = DepositPerSeat;
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            model.CustomerId = userId;
            model.Status = BookingStatus.ChoXacNhan;
            // Tính tiền đặt bàn/đặt ghế = số ghế x đơn giá cọc mỗi ghế
            model.DepositAmount = model.NumberOfGuests * DepositPerSeat;
            model.CreatedAt = DateTime.Now;

            _context.Bookings.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đặt bàn thành công! Tiền cọc cần thanh toán: {model.DepositAmount:N0} VNĐ. Vui lòng chờ nhân viên xác nhận.";
            return RedirectToAction(nameof(MyBookings));
        }

        // GET: Booking/MyBookings - danh sách đặt bàn của tôi
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.Bookings
                .Include(b => b.Table)
                .Where(b => b.CustomerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookings);
        }

        // POST: Booking/Cancel/5 - khách tự huỷ đặt bàn của mình (chỉ khi chưa xác nhận)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == userId);
            if (booking == null) return NotFound();

            if (booking.Status == BookingStatus.ChoXacNhan || booking.Status == BookingStatus.DaXacNhan)
            {
                booking.Status = BookingStatus.DaHuy;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã huỷ đặt bàn.";
            }
            else
            {
                TempData["Error"] = "Không thể huỷ đặt bàn ở trạng thái hiện tại.";
            }
            return RedirectToAction(nameof(MyBookings));
        }

        // API nhỏ: lấy sức chứa của bàn để hiển thị gợi ý tiền cọc bằng JS (không bắt buộc)
        [HttpGet]
        public async Task<IActionResult> GetTableInfo(int tableId)
        {
            var table = await _context.RestaurantTables.FindAsync(tableId);
            if (table == null) return NotFound();
            return Json(new { capacity = table.Capacity, status = table.Status.ToString() });
        }
    }
}
