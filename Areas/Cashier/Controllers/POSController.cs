using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Areas.Cashier.ViewModels;
using Quản_lý_quán_cafe.Data;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Models;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Services;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    [SessionAuthorize("Cashier,Admin")]
    public class POSController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<POSController> _logger;
        private readonly IApplicationMutationCoordinator _mutationCoordinator;
        private readonly ILoyaltyService _loyaltyService;

        public POSController(
            ApplicationDbContext context,
            ILogger<POSController> logger,
            IApplicationMutationCoordinator mutationCoordinator,
            ILoyaltyService loyaltyService)
        {
            _context = context;
            _logger = logger;
            _mutationCoordinator = mutationCoordinator;
            _loyaltyService = loyaltyService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? tableId = null)
        {
            if (!IsStaff()) return RedirectToAction("Login", "Account", new { area = "" });
            var viewModel = new POSViewModel();

            viewModel.OpenTables = await BuildOpenTablesAsync(tableId);

            viewModel.Products = await _context.Products.AsNoTracking()
                .Where(p => !p.IsDeleted && p.IsActive && p.Quantity > 0
                    && p.Category != null && !p.Category.IsDeleted && p.Category.IsActive)
                .OrderBy(p => p.Category!.CategoryName)
                .ThenBy(p => p.ProductName)
                .Select(p => new POSProductViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Quantity = p.Quantity,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Khác"
                })
                .ToListAsync();

            if (viewModel.OpenTables.Any() && tableId.HasValue)
            {
                var selectedTable = viewModel.OpenTables.FirstOrDefault(t => t.TableID == tableId);
                if (selectedTable != null)
                {
                    selectedTable.IsSelected = true;
                    viewModel.CurrentTable = selectedTable;
                    await PopulateOrderAsync(viewModel, selectedTable.TableID);
                }
            }

            return View(viewModel);
        }

        private async Task<List<POSTableViewModel>> BuildOpenTablesAsync(int? selectedTableId = null)
        {
            var now = BusinessClock.Now;
            var holdCutoff = now.AddMinutes(ReservationPolicy.HoldBeforeMinutes);
            var oldestActiveStart = now.AddMinutes(-ReservationPolicy.DurationMinutes);
            var openTables = await _context.RestaurantTables
                .Where(t => !t.IsDeleted && t.TableStatus != "Maintenance")
                .Include(t => t.Orders.Where(o => !o.IsDeleted &&
                    o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" &&
                    o.OrderDetails.Any(d => !d.IsDeleted)))
                    .ThenInclude(o => o.OrderDetails.Where(d => !d.IsDeleted))
                .Include(t => t.Reservations.Where(r => !r.IsDeleted &&
                    r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" && r.ReservationStatus != "CheckedIn" &&
                    r.ReservationDate <= holdCutoff && r.ReservationDate > oldestActiveStart))
                .AsSplitQuery()
                .ToListAsync();

            return openTables.Select(t =>
            {
                var activeOrder = t.Orders.FirstOrDefault();
                var displayedStatus = activeOrder?.OrderStatus == "WaitingPayment"
                    ? "WaitingPayment"
                    : t.Reservations.Any() && t.TableStatus == "Available" ? "Reserved" : t.TableStatus;
                return new POSTableViewModel
                {
                    OrderID = activeOrder?.OrderID,
                    OrderStatus = activeOrder?.OrderStatus,
                    TableID = t.TableID,
                    TableNumber = t.TableNumber,
                    TableName = t.TableNumber,
                    OrderCode = activeOrder is not null ? $"#{activeOrder.OrderID}" : string.Empty,
                    ItemCount = activeOrder?.OrderDetails.Sum(d => d.Quantity) ?? 0,
                    TotalAmount = activeOrder?.TotalAmount ?? 0,
                    Status = displayedStatus.ToLowerInvariant(),
                    StatusBadge = displayedStatus switch
                    {
                        "WaitingPayment" => "THANH TOÁN",
                        "Reserved" => "ĐÃ ĐẶT",
                        _ => string.Empty
                    },
                    IsSelected = t.TableID == selectedTableId
                };
            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> OpenTables(int? selectedTableId = null)
        {
            if (!IsStaff()) return StatusCode(403);
            var model = await BuildOpenTablesAsync(selectedTableId);
            return PartialView("~/Areas/Cashier/Views/Shared/_TableList.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> SelectTable(int tableId)
        {
            if (!IsStaff()) return StatusCode(403);
            var table = await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableID == tableId && !t.IsDeleted);

            if (table == null)
                return NotFound();

            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.TableID == tableId && !o.IsDeleted &&
                                          o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" &&
                                          o.OrderDetails.Any(d => !d.IsDeleted));
            var orderItems = order?.OrderDetails.Select(ToOrderItem).ToList() ?? new();

            return Json(new
            {
                table = new
                {
                    tableID = table.TableID,
                    tableName = table.TableNumber,
                    orderCode = order == null ? string.Empty : $"#{order.OrderID}",
                    status = table.TableStatus.ToLower()
                },
                items = orderItems,
                subtotal = orderItems.Sum(i => i.Total)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(int tableId, int productId, int quantity, string size = "M", string notes = "")
        {
            if (!IsStaff()) return StatusCode(403);
            await using var mutationLock = await _mutationCoordinator.EnterAsync(HttpContext.RequestAborted);
            try
            {
                if (quantity < 1) return BadRequest(new { success = false, message = "Số lượng phải lớn hơn 0" });
                var table = await _context.RestaurantTables.FirstOrDefaultAsync(t => t.TableID == tableId && !t.IsDeleted);
                notes = notes?.Trim() ?? string.Empty;
                if (notes.Length > 500)
                    return BadRequest(new { success = false, message = "Ghi chú món không được vượt quá 500 ký tự." });
                var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductID == productId && p.IsActive && !p.IsDeleted &&
                    p.Category != null && p.Category.IsActive && !p.Category.IsDeleted);
                if (table == null || product == null) return NotFound(new { success = false, message = "Không tìm thấy bàn hoặc sản phẩm" });
                if (table.TableStatus == "Maintenance") return Conflict(new { success = false, message = "Bàn đang bảo trì." });
                if (product.Quantity < quantity) return Conflict(new { success = false, message = "Sản phẩm không đủ số lượng" });

                var order = await _context.Orders.Include(o => o.OrderDetails).Include(o => o.Payment)
                    .FirstOrDefaultAsync(o => o.TableID == tableId && !o.IsDeleted &&
                                              o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled");
                if (order is not null && order.OrderStatus != "Pending")
                    return Conflict(new { success = false, message = "Đơn đã chuyển sang xử lý hoặc chờ thanh toán, không thể thêm món." });
                if (order == null || !order.OrderDetails.Any(d => !d.IsDeleted))
                {
                    var now = BusinessClock.Now;
                    var holdCutoff = now.AddMinutes(ReservationPolicy.HoldBeforeMinutes);
                    var oldestActiveStart = now.AddMinutes(-ReservationPolicy.DurationMinutes);
                    var activeReservations = await _context.Reservations
                        .Where(r =>
                        r.TableID == tableId && !r.IsDeleted &&
                        (r.ReservationStatus == "Pending" || r.ReservationStatus == "Confirmed" || r.ReservationStatus == "CheckedIn") &&
                        r.ReservationDate <= holdCutoff && r.ReservationDate > oldestActiveStart)
                        .OrderBy(r => r.ReservationDate)
                        .ToListAsync();
                    if (activeReservations.Count > 1)
                        return Conflict(new { success = false, message = "Bàn có nhiều lịch đặt đang hiệu lực. Vui lòng xử lý lịch đặt trước." });
                    var activeReservation = activeReservations.SingleOrDefault();

                    if (order == null)
                    {
                        order = new Models.Entities.Order
                        {
                            TableID = tableId,
                            CustomerID = activeReservation?.CustomerID,
                            EmployeeID = await GetCurrentEmployeeIdAsync(),
                            OrderStatus = "Pending",
                            OrderDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Orders.Add(order);
                    }
                    else if (activeReservation is not null)
                    {
                        if (order.CustomerID.HasValue && order.CustomerID != activeReservation.CustomerID)
                            return Conflict(new { success = false, message = "Đơn hiện tại không thuộc khách đã đặt bàn." });
                        order.CustomerID = activeReservation.CustomerID;
                    }

                    if (activeReservation is not null && activeReservation.ReservationStatus != "CheckedIn")
                    {
                        activeReservation.ReservationStatus = "CheckedIn";
                        activeReservation.CheckinTime = DateTime.UtcNow;
                        activeReservation.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else if (!order.CustomerID.HasValue)
                {
                    var now = BusinessClock.Now;
                    var checkedInReservation = await _context.Reservations
                        .Where(r => r.TableID == tableId && !r.IsDeleted && r.ReservationStatus == "CheckedIn" &&
                                    r.ReservationDate <= now.AddMinutes(ReservationPolicy.HoldBeforeMinutes) &&
                                    r.ReservationDate > now.AddMinutes(-ReservationPolicy.DurationMinutes))
                        .OrderByDescending(r => r.ReservationDate)
                        .FirstOrDefaultAsync();
                    if (checkedInReservation is not null) order.CustomerID = checkedInReservation.CustomerID;
                }

                var detail = order.OrderDetails.FirstOrDefault(d => d.ProductID == productId && !d.IsDeleted && d.Notes == notes);
                if (detail == null)
                {
                    detail = new Models.Entities.OrderDetail { ProductID = productId, Quantity = quantity, UnitPrice = product.Price, Subtotal = product.Price * quantity, Notes = notes, CreatedAt = DateTime.UtcNow };
                    order.OrderDetails.Add(detail);
                }
                else
                {
                    detail.Quantity += quantity;
                    detail.Subtotal = detail.Quantity * detail.UnitPrice;
                    detail.UpdatedAt = DateTime.UtcNow;
                }
                product.Quantity -= quantity;
                table.TableStatus = "Occupied";
                order.SubtotalAmount = order.OrderDetails.Where(d => !d.IsDeleted).Sum(d => d.Subtotal);
                order.TotalAmount = order.SubtotalAmount;
                order.VoucherDiscountAmount = 0;
                order.PointDiscountAmount = 0;
                order.UpdatedAt = DateTime.UtcNow;
                if (order.OrderID > 0)
                {
                    await _loyaltyService.ResetDiscountForChangedOrderAsync(
                        order.OrderID, HttpContext.RequestAborted);
                }
                else if (order.Payment?.PaymentStatus == "Pending")
                {
                    order.Payment.Amount = order.TotalAmount;
                    order.Payment.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã thêm món", orderId = order.OrderID, totalAmount = order.TotalAmount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not add product {ProductId} to table {TableId}.", productId, tableId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Không thể thêm món. Vui lòng thử lại." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem(int orderDetailId, int quantity)
        {
            if (!IsStaff()) return StatusCode(403);
            await using var mutationLock = await _mutationCoordinator.EnterAsync(HttpContext.RequestAborted);
            try
            {
                if (quantity < 0) return BadRequest(new { success = false, message = "Số lượng không được âm." });
                var detail = await _context.OrderDetails
                    .Include(d => d.Order)!.ThenInclude(o => o!.OrderDetails)
                    .Include(d => d.Order)!.ThenInclude(o => o!.Table)
                    .Include(d => d.Order)!.ThenInclude(o => o!.Payment)
                    .Include(d => d.Product)
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
                detail.Order.SubtotalAmount = detail.Order.OrderDetails.Where(d => !d.IsDeleted).Sum(d => d.Subtotal);
                detail.Order.TotalAmount = detail.Order.SubtotalAmount;
                await _loyaltyService.ResetDiscountForChangedOrderAsync(
                    detail.Order.OrderID, HttpContext.RequestAborted);
                var remaining = detail.Order.OrderDetails.Count(d => !d.IsDeleted);
                if (remaining == 0 && detail.Order.Table != null)
                {
                    detail.Order.OrderStatus = "Cancelled";
                    if (detail.Order.Payment?.PaymentStatus == "Pending")
                        detail.Order.Payment.PaymentStatus = "Failed";
                    var hasCheckedInReservation = await HasCurrentCheckedInReservationAsync(
                        detail.Order.Table.TableID, detail.Order.CustomerID);
                    detail.Order.Table.TableStatus = hasCheckedInReservation ? "Occupied" : "Available";
                    detail.Order.Table.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, totalAmount = detail.Order.TotalAmount, orderCancelled = remaining == 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not update order detail {OrderDetailId}.", orderDetailId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Không thể cập nhật món. Vui lòng thử lại." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int orderDetailId)
        {
            if (!IsStaff()) return StatusCode(403);
            await using var mutationLock = await _mutationCoordinator.EnterAsync(HttpContext.RequestAborted);
            try
            {
                var detail = await _context.OrderDetails
                    .Include(d => d.Order)!.ThenInclude(o => o!.OrderDetails)
                    .Include(d => d.Order)!.ThenInclude(o => o!.Table)
                    .Include(d => d.Order)!.ThenInclude(o => o!.Payment)
                    .Include(d => d.Product)
                    .FirstOrDefaultAsync(d => d.OrderDetailID == orderDetailId && !d.IsDeleted);
                if (detail == null || detail.Order?.OrderStatus != "Pending") return NotFound(new { success = false });
                detail.IsDeleted = true;
                if (detail.Product != null) detail.Product.Quantity += detail.Quantity;
                detail.Order.SubtotalAmount = detail.Order.OrderDetails.Where(d => !d.IsDeleted).Sum(d => d.Subtotal);
                detail.Order.TotalAmount = detail.Order.SubtotalAmount;
                await _loyaltyService.ResetDiscountForChangedOrderAsync(
                    detail.Order.OrderID, HttpContext.RequestAborted);
                var remaining = detail.Order.OrderDetails.Count(d => !d.IsDeleted);
                if (remaining == 0 && detail.Order.Table != null)
                {
                    detail.Order.OrderStatus = "Cancelled";
                    if (detail.Order.Payment?.PaymentStatus == "Pending")
                        detail.Order.Payment.PaymentStatus = "Failed";
                    var hasCheckedInReservation = await HasCurrentCheckedInReservationAsync(
                        detail.Order.Table.TableID, detail.Order.CustomerID);
                    detail.Order.Table.TableStatus = hasCheckedInReservation ? "Occupied" : "Available";
                    detail.Order.Table.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();

                return Json(new { success = true, totalAmount = detail.Order.TotalAmount, orderCancelled = remaining == 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not remove order detail {OrderDetailId}.", orderDetailId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Không thể xóa món. Vui lòng thử lại." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomer(string phone)
        {
            if (!IsStaff()) return StatusCode(403);
            try
            {
                var customer = await _context.Customers
                    .AsNoTracking()
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Phone == phone && !c.IsDeleted && c.IsActive
                        && c.User != null && !c.User.IsDeleted && c.User.IsActive);

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
                        username = customer.User?.Username,
                        rewardPoints = customer.RewardPoints
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not search customer by phone.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Không thể tìm khách hàng. Vui lòng thử lại." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            int tableId,
            string paymentMethod,
            decimal? amountReceived = null,
            decimal? paidAmount = null,
            string? notes = null,
            int? expectedOrderId = null,
            decimal? expectedTotal = null,
            decimal? expectedAmount = null,
            int? loyaltyCustomerId = null)
        {
            if (!IsStaff()) return StatusCode(403);
            await using var mutationLock = await _mutationCoordinator.EnterAsync(HttpContext.RequestAborted);
            try
            {
                var activeOrders = await _context.Orders.Include(o => o.OrderDetails).Include(o => o.Table).Include(o => o.Payment)
                    .Where(o => o.TableID == tableId && !o.IsDeleted &&
                                o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" &&
                                o.OrderDetails.Any(d => !d.IsDeleted))
                    .ToListAsync();
                if (activeOrders.Count > 1)
                    return Conflict(new { success = false, message = "Bàn có nhiều đơn đang mở. Vui lòng xử lý từng đơn trong danh sách đơn hàng." });
                var order = activeOrders.SingleOrDefault();
                if (order == null || !order.OrderDetails.Any(d => !d.IsDeleted)) return Conflict(new { success = false, message = "Không có đơn hàng để thanh toán" });
                if (expectedOrderId.HasValue && order.OrderID != expectedOrderId.Value)
                    return Conflict(new { success = false, message = "Đơn hàng đã thay đổi. Vui lòng tạo lại mã QR." });
                if (order.Payment?.PaymentStatus == "Completed")
                    return Conflict(new { success = false, message = "Đơn hàng đã được thanh toán" });
                var normalizedPaymentMethod = paymentMethod?.Trim().ToLowerInvariant();
                if (normalizedPaymentMethod is not ("cash" or "qr" or "discount"))
                    return BadRequest(new { success = false, message = "Phương thức thanh toán không hợp lệ" });
                notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
                if (notes?.Length > 1000)
                    return BadRequest(new { success = false, message = "Ghi chú không được vượt quá 1000 ký tự" });
                if (loyaltyCustomerId.HasValue)
                {
                    var effectiveLoyaltyCustomerId = order.CustomerID.HasValue
                        && !order.IsLoyaltyCustomerAssigned
                            ? order.CustomerID.Value
                            : loyaltyCustomerId.Value;
                    var loyaltyCustomer = await _context.Customers
                        .Include(customer => customer.User)
                        .SingleOrDefaultAsync(customer =>
                            customer.CustomerID == effectiveLoyaltyCustomerId
                            && !customer.IsDeleted && customer.IsActive
                            && customer.User != null && !customer.User.IsDeleted && customer.User.IsActive);
                    if (loyaltyCustomer is null)
                        return BadRequest(new { success = false, message = "Tài khoản tích điểm không còn hoạt động." });

                    if (!order.CustomerID.HasValue || order.IsLoyaltyCustomerAssigned)
                    {
                        order.CustomerID = loyaltyCustomer.CustomerID;
                        order.Customer = loyaltyCustomer;
                        order.IsLoyaltyCustomerAssigned = true;
                    }
                }
                var actorUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
                var quote = await _loyaltyService.PrepareCheckoutAsync(
                    order.OrderID,
                    actorUserId,
                    HttpContext.RequestAborted);
                var expected = expectedTotal ?? expectedAmount;
                if (expected.HasValue && expected.Value != quote.TotalAmount)
                    return Conflict(new { success = false, message = "Số tiền đơn hàng đã thay đổi. Vui lòng tạo lại mã QR." });
                var tendered = amountReceived ?? paidAmount ?? 0;
                if (tendered < quote.TotalAmount)
                    return BadRequest(new { success = false, message = "Số tiền thanh toán chưa đủ" });
                if (normalizedPaymentMethod == "discount" && quote.TotalAmount != 0)
                    return BadRequest(new { success = false, message = "Chỉ đơn hàng 0 ₫ mới được bỏ qua thanh toán." });
                Models.Entities.Customer? customer = null;
                if (order.CustomerID.HasValue)
                {
                    customer = await _context.Customers.FirstOrDefaultAsync(c =>
                        c.CustomerID == order.CustomerID && !c.IsDeleted && c.IsActive);
                    if (customer == null) return BadRequest(new { success = false, message = "Khách hàng của đơn không còn hợp lệ" });
                    customer.TotalSpent += quote.TotalAmount;
                    customer.UpdatedAt = DateTime.UtcNow;
                }
                if (order.CustomerID.HasValue)
                {
                    var reservationNow = BusinessClock.Now;
                    var checkedInReservation = await _context.Reservations
                        .Where(r =>
                        r.TableID == tableId && r.CustomerID == order.CustomerID && !r.IsDeleted &&
                        r.ReservationStatus == "CheckedIn" &&
                        r.ReservationDate <= reservationNow.AddMinutes(ReservationPolicy.HoldBeforeMinutes) &&
                        r.ReservationDate > reservationNow.AddMinutes(-ReservationPolicy.DurationMinutes))
                        .OrderByDescending(r => r.ReservationDate)
                        .FirstOrDefaultAsync();
                    if (checkedInReservation is not null)
                    {
                        checkedInReservation.ReservationStatus = "Completed";
                        checkedInReservation.CheckoutTime = DateTime.UtcNow;
                        checkedInReservation.UpdatedAt = DateTime.UtcNow;
                    }
                }
                order.Notes = notes;
                order.EmployeeID ??= await GetCurrentEmployeeIdAsync();
                var payment = order.Payment ?? new Models.Entities.Payment
                {
                    OrderID = order.OrderID,
                    CreatedAt = DateTime.UtcNow
                };
                payment.Amount = quote.TotalAmount;
                payment.PaymentMethod = quote.TotalAmount == 0
                    ? "Discount"
                    : normalizedPaymentMethod == "qr" ? "QR" : "Cash";
                payment.PaymentStatus = "Completed";
                payment.PaymentDate = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;
                if (order.Payment is null)
                {
                    order.Payment = payment;
                    _context.Payments.Add(payment);
                }
                order.OrderStatus = "Completed";
                order.CompletedDate = DateTime.UtcNow;
                order.UpdatedAt = DateTime.UtcNow;
                if (order.Table != null) { order.Table.TableStatus = "Available"; order.Table.UpdatedAt = DateTime.UtcNow; }
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = quote.TotalAmount == 0
                        ? "Đơn 0 ₫ đã hoàn tất. Đang mở hóa đơn."
                        : "Thanh toán thành công",
                    orderId = order.OrderID,
                    amount = quote.TotalAmount,
                    change = tendered - quote.TotalAmount,
                    earnedPoints = quote.EarnedPoints
                });
            }
            catch (LoyaltyRuleException ex)
            {
                return StatusCode(ex.StatusCode, new { success = false, message = ex.Message });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Point balance changed while checking out table {TableId}.", tableId);
                return Conflict(new
                {
                    success = false,
                    message = "Số dư điểm vừa được sử dụng ở giao dịch khác. Vui lòng tải lại hóa đơn."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not checkout table {TableId}.", tableId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { success = false, message = "Không thể hoàn tất thanh toán. Vui lòng thử lại." });
            }
        }

        private bool IsStaff()
        {
            var role = HttpContext.Session.GetString("RoleName");
            return role is "Admin" or "Cashier";
        }

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return null;
            return await _context.Users.AsNoTracking()
                .Where(u => u.UserID == userId.Value && !u.IsDeleted)
                .Select(u => u.EmployeeID)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> HasCurrentCheckedInReservationAsync(int tableId, int? customerId)
        {
            var now = BusinessClock.Now;
            return await _context.Reservations.AnyAsync(r =>
                r.TableID == tableId && !r.IsDeleted && r.ReservationStatus == "CheckedIn" &&
                (!customerId.HasValue || r.CustomerID == customerId.Value) &&
                r.ReservationDate <= now.AddMinutes(ReservationPolicy.HoldBeforeMinutes) &&
                r.ReservationDate > now.AddMinutes(-ReservationPolicy.DurationMinutes));
        }

        private async Task PopulateOrderAsync(POSViewModel viewModel, int tableId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.TableID == tableId && !o.IsDeleted &&
                                          o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled" &&
                                          o.OrderDetails.Any(d => !d.IsDeleted));
            if (order == null) return;

            viewModel.OrderItems = order.OrderDetails.Select(ToOrderItem).ToList();
            var quote = await _loyaltyService.GetOrderQuoteAsync(order.OrderID, HttpContext.RequestAborted);
            viewModel.Subtotal = quote.SubtotalAmount;
            viewModel.DiscountAmount = quote.DiscountAmount;
            viewModel.Total = quote.TotalAmount;
            viewModel.EarnedPoints = quote.EarnedPoints;
            viewModel.DiscountMode = quote.Mode;
            viewModel.VoucherCode = quote.VoucherCode;
            viewModel.DiscountAccounts = quote.Accounts.Select(account => new POSDiscountAccountViewModel
            {
                CustomerID = account.CustomerId,
                Account = account.Username,
                Name = account.Name,
                Phone = account.Phone,
                AvailablePoints = account.AvailablePoints,
                PointsUsed = account.PointsUsed,
                DiscountAmount = account.DiscountAmount
            }).ToList();
            viewModel.Notes = order.Notes ?? string.Empty;
            if (order.Customer != null)
            {
                viewModel.Customer = new POSCustomerViewModel
                {
                    CustomerID = order.Customer.CustomerID,
                    Name = order.Customer.CustomerName,
                    Phone = order.Customer.Phone ?? string.Empty,
                    Email = order.Customer.Email ?? string.Empty,
                    RewardPoints = order.Customer.RewardPoints
                };
            }
        }

        private static POSOrderItemViewModel ToOrderItem(Models.Entities.OrderDetail detail) => new()
        {
            OrderDetailID = detail.OrderDetailID,
            ProductID = detail.ProductID,
            ProductName = detail.Product?.ProductName ?? $"Sản phẩm #{detail.ProductID}",
            Price = detail.UnitPrice,
            Quantity = detail.Quantity,
            Notes = detail.Notes ?? string.Empty
        };
    }
}
