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
            if (!IsStaff()) return RedirectToAction("Login", "Account", new { area = "" });
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
            if (!IsStaff()) return StatusCode(403);
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
            if (!IsStaff()) return StatusCode(403);
            try
            {
                if (quantity < 1) return BadRequest(new { success = false, message = "Số lượng phải lớn hơn 0" });
                var table = await _context.RestaurantTables.FirstOrDefaultAsync(t => t.TableID == tableId && !t.IsDeleted);
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductID == productId && p.IsActive && !p.IsDeleted);
                if (table == null || product == null) return NotFound(new { success = false, message = "Không tìm thấy bàn hoặc sản phẩm" });
                if (product.Quantity < quantity) return Conflict(new { success = false, message = "Sản phẩm không đủ số lượng" });

                var order = await _context.Orders.Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.TableID == tableId && !o.IsDeleted && o.OrderStatus == "Pending");
                if (order == null)
                {
                    order = new Models.Entities.Order { TableID = tableId, OrderStatus = "Pending", OrderDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();
                }

                var detail = order.OrderDetails.FirstOrDefault(d => d.ProductID == productId && !d.IsDeleted && d.Notes == notes);
                if (detail == null)
                    _context.OrderDetails.Add(new Models.Entities.OrderDetail { OrderID = order.OrderID, ProductID = productId, Quantity = quantity, UnitPrice = product.Price, Subtotal = product.Price * quantity, Notes = notes, CreatedAt = DateTime.UtcNow });
                else
                {
                    detail.Quantity += quantity;
                    detail.Subtotal = detail.Quantity * detail.UnitPrice;
                    detail.UpdatedAt = DateTime.UtcNow;
                }
                product.Quantity -= quantity;
                table.TableStatus = "Occupied";
                order.TotalAmount = order.OrderDetails.Where(d => !d.IsDeleted).Sum(d => d.Subtotal) + (detail == null ? product.Price * quantity : 0);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã thêm món", orderId = order.OrderID, totalAmount = order.TotalAmount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItem(int orderDetailId, int quantity)
        {
            if (!IsStaff()) return StatusCode(403);
            try
            {
                var detail = await _context.OrderDetails.Include(d => d.Order)!.ThenInclude(o => o!.OrderDetails).Include(d => d.Product)
                    .FirstOrDefaultAsync(d => d.OrderDetailID == orderDetailId && !d.IsDeleted);
                if (detail == null || detail.Order?.OrderStatus != "Pending") return NotFound(new { success = false });
                var difference = quantity - detail.Quantity;
                if (quantity <= 0) detail.IsDeleted = true;
                else
                {
                    if (difference > 0 && (detail.Product?.Quantity ?? 0) < difference) return Conflict(new { success = false, message = "Không đủ tồn kho" });
                    detail.Quantity = quantity;
                    detail.Subtotal = detail.UnitPrice * quantity;
                }
                if (detail.Product != null) detail.Product.Quantity -= difference;
                detail.Order.TotalAmount = detail.Order.OrderDetails.Where(d => !d.IsDeleted).Sum(d => d.Subtotal);
                await _context.SaveChangesAsync();
                return Json(new { success = true, totalAmount = detail.Order.TotalAmount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int orderDetailId)
        {
            if (!IsStaff()) return StatusCode(403);
            try
            {
                var detail = await _context.OrderDetails.Include(d => d.Order)!.ThenInclude(o => o!.OrderDetails).Include(d => d.Product)
                    .FirstOrDefaultAsync(d => d.OrderDetailID == orderDetailId && !d.IsDeleted);
                if (detail == null || detail.Order?.OrderStatus != "Pending") return NotFound(new { success = false });
                detail.IsDeleted = true;
                if (detail.Product != null) detail.Product.Quantity += detail.Quantity;
                detail.Order.TotalAmount = detail.Order.OrderDetails.Where(d => !d.IsDeleted).Sum(d => d.Subtotal);
                await _context.SaveChangesAsync();
                return Json(new { success = true, totalAmount = detail.Order.TotalAmount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomer(string phone)
        {
            if (!IsStaff()) return StatusCode(403);
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
            if (!IsStaff()) return StatusCode(403);
            try
            {
                var order = await _context.Orders.Include(o => o.OrderDetails).Include(o => o.Table)
                    .FirstOrDefaultAsync(o => o.TableID == tableId && !o.IsDeleted && o.OrderStatus == "Pending");
                if (order == null || !order.OrderDetails.Any(d => !d.IsDeleted)) return Conflict(new { success = false, message = "Không có đơn hàng để thanh toán" });
                order.TotalAmount = order.OrderDetails.Where(d => !d.IsDeleted).Sum(d => d.Subtotal);
                if (paidAmount < order.TotalAmount) return BadRequest(new { success = false, message = "Số tiền thanh toán chưa đủ" });
                var payment = new Models.Entities.Payment { OrderID = order.OrderID, Amount = order.TotalAmount, PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash" : paymentMethod, PaymentStatus = "Completed", PaymentDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
                _context.Payments.Add(payment);
                order.OrderStatus = "Completed";
                order.CompletedDate = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                if (order.Table != null) { order.Table.TableStatus = "Available"; order.Table.UpdatedAt = DateTime.UtcNow; }
                await _context.SaveChangesAsync();
                order.PaymentID = payment.PaymentID;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thanh toán thành công", orderId = order.OrderID, amount = order.TotalAmount, change = paidAmount - order.TotalAmount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private bool IsStaff()
        {
            var role = HttpContext.Session.GetString("RoleName");
            return role is "Admin" or "Cashier";
        }
    }
}

