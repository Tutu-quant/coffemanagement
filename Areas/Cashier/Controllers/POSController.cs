using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Areas.Cashier.ViewModels;
using Quản_lý_quán_cafe.Data;
using Microsoft.EntityFrameworkCore;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    public class POSController : Controller
    {
        private readonly ApplicationDbContext _context;

        public POSController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var viewModel = new POSViewModel();

            // Lấy danh sách bàn đang mở (Occupied hoặc WaitingPayment theo TableStatus)
            var openTables = await _context.RestaurantTables
                .Where(t => !t.IsDeleted && (t.TableStatus == "Occupied" || t.TableStatus == "WaitingPayment"))
                .ToListAsync();

            viewModel.OpenTables = openTables.Select(t => new POSTableViewModel
            {
                TableID = t.TableID,
                TableNumber = t.TableNumber,
                TableName = t.TableNumber, // TODO: Check if need separate TableName field
                OrderCode = "#" + t.TableID, // TODO: Lấy từ Order.OrderCode thực tế
                ItemCount = 0, // TODO: Tính từ OrderDetails
                TotalAmount = 0, // TODO: Tính từ Order.Total
                Status = t.TableStatus.ToLower(),
                StatusBadge = t.TableStatus == "WaitingPayment" ? "THANH TOÁN" : ""
            }).ToList();

            // Mặc định chọn bàn đầu tiên (hoặc từ session)
            if (viewModel.OpenTables.Any())
            {
                var selectedTable = viewModel.OpenTables.First();
                viewModel.CurrentTable = selectedTable;
                // TODO: Lấy Order Items từ database
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> SelectTable(int tableId)
        {
            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableID == tableId && !t.IsDeleted);

            if (table == null)
                return NotFound();

            // TODO: Lấy Order Items của bàn này
            var orderItems = new List<POSOrderItemViewModel>();

            return Json(new
            {
                table = new
                {
                    tableID = table.TableID,
                    tableName = table.TableNumber,
                    orderCode = "#" + table.TableID,
                    status = table.TableStatus.ToLower()
                },
                items = orderItems
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(int tableId, int productId, int quantity, string size = "M", string notes = "")
        {
            try
            {
                // TODO: Thêm OrderDetail vào database
                return Json(new { success = true, message = "Đã thêm món" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItem(int orderDetailId, int quantity)
        {
            try
            {
                // TODO: Cập nhật số lượng OrderDetail
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int orderDetailId)
        {
            try
            {
                // TODO: Xóa OrderDetail
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomer(string phone)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Phone == phone && !c.IsDeleted);

                if (customer == null)
                    return Json(new { found = false });

                return Json(new
                {
                    found = true,
                    customer = new
                    {
                        customerID = customer.CustomerID,
                        name = customer.CustomerName,
                        phone = customer.Phone,
                        rewardPoints = customer.RewardPoints,
                        membershipTier = customer.MembershipTier
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(int tableId, string paymentMethod, decimal paidAmount)
        {
            try
            {
                // TODO: Tạo Payment record và cập nhật Order status
                return Json(new { success = true, message = "Thanh toán thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

