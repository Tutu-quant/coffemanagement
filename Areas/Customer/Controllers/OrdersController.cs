using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Areas.Customer.ViewModels;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Services;
using Quản_lý_quán_cafe.Models;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Areas.Customer.Controllers;

[Area("Customer")]
[SessionAuthorize("Customer")]
public class OrdersController(
    ApplicationDbContext context,
    Quản_lý_quán_cafe.Services.CustomerSessionService customerSessionService,
    IApplicationMutationCoordinator mutationCoordinator,
    ILoyaltyService loyaltyService,
    ILogger<OrdersController> logger) : Controller
{
    private readonly Quản_lý_quán_cafe.Services.CustomerSessionService _customerSessionService = customerSessionService;
    private readonly IApplicationMutationCoordinator _mutationCoordinator = mutationCoordinator;
    private readonly ILoyaltyService _loyaltyService = loyaltyService;
    private readonly ILogger<OrdersController> _logger = logger;
    [HttpGet]
    public async Task<IActionResult> Index(int? tableId = null)
    {
        if (!IsCustomer()) return RedirectToLogin();
        var model = new OrderMenuViewModel { TableId = tableId ?? 0 };
        await LoadMenuAsync(model);
        return View("Menu", model);
    }

    [HttpGet]
    public Task<IActionResult> Menu(int? tableId = null) => Index(tableId);

    [HttpGet]
    public Task<IActionResult> Place(int? tableId = null) => Index(tableId);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitOrder(OrderMenuViewModel model)
    {
        if (!IsCustomer()) return RedirectToLogin();

        List<CartItemInput>? cart;
        try
        {
            cart = JsonSerializer.Deserialize<List<CartItemInput>>(model.CartJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            cart = null;
        }

        if (cart is not null)
        {
            if (cart.Any(x => x.ProductId <= 0 || x.Quantity <= 0 || x.Quantity > 10_000))
                ModelState.AddModelError(nameof(model.CartJson), "Số lượng món trong giỏ hàng không hợp lệ.");
            if (cart.Any(x => x.Notes?.Trim().Length > 500))
                ModelState.AddModelError(nameof(model.CartJson), "Ghi chú món không được vượt quá 500 ký tự.");

            var groupedCart = cart
                .Where(x => x.ProductId > 0 && x.Quantity > 0 && x.Quantity <= 10_000)
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => (long)x.Quantity),
                    Notes = g.Last().Notes
                })
                .ToList();
            if (groupedCart.Any(x => x.Quantity > 10_000))
                ModelState.AddModelError(nameof(model.CartJson), "Số lượng món trong giỏ hàng vượt giới hạn.");
            cart = groupedCart
                .Where(x => x.Quantity <= 10_000)
                .Select(x => new CartItemInput(x.ProductId, (int)x.Quantity, x.Notes))
                .ToList();
        }

        await using var mutationLock = await _mutationCoordinator.EnterAsync(HttpContext.RequestAborted);

        var table = await context.RestaurantTables
            .FirstOrDefaultAsync(t => t.TableID == model.TableId && !t.IsDeleted);
        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        Reservation? matchingReservation = null;
        if (table is null || table.TableStatus == TableStatus.Maintenance)
            ModelState.AddModelError(nameof(model.TableId), "Vui lòng chọn một bàn đang phục vụ.");
        if (table is not null)
        {
            var now = BusinessClock.Now;
            var holdCutoff = now.AddMinutes(ReservationPolicy.HoldBeforeMinutes);
            var oldestActiveStart = now.AddMinutes(-ReservationPolicy.DurationMinutes);
            matchingReservation = await context.Reservations.FirstOrDefaultAsync(r =>
                r.TableID == table.TableID && r.CustomerID == customer.CustomerID && !r.IsDeleted &&
                (r.ReservationStatus == "Pending" || r.ReservationStatus == "Confirmed" ||
                 r.ReservationStatus == "CheckedIn") &&
                r.ReservationDate <= holdCutoff && r.ReservationDate > oldestActiveStart);
            var heldForAnotherCustomer = await context.Reservations.AnyAsync(r =>
                r.TableID == table.TableID && r.CustomerID != customer.CustomerID && !r.IsDeleted &&
                r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" &&
                r.ReservationDate <= holdCutoff && r.ReservationDate > oldestActiveStart);
            if (heldForAnotherCustomer)
                ModelState.AddModelError(nameof(model.TableId), "Bàn đang được giữ cho khách đã đặt trước.");
            else if (table.TableStatus != TableStatus.Available && matchingReservation is null)
                ModelState.AddModelError(nameof(model.TableId), "Bàn hiện không sẵn sàng phục vụ.");
        }
        if (cart is null || cart.Count == 0)
            ModelState.AddModelError(nameof(model.CartJson), "Giỏ hàng chưa có món.");

        if (!ModelState.IsValid)
        {
            await LoadMenuAsync(model);
            return View("Menu", model);
        }

        var validCart = cart!;
        var hasOpenOrder = await context.Orders.AnyAsync(o => o.TableID == table!.TableID && !o.IsDeleted &&
            o.OrderStatus != OrderStatusConstants.Completed && o.OrderStatus != OrderStatusConstants.Cancelled &&
            o.OrderDetails.Any(d => !d.IsDeleted));
        if (hasOpenOrder)
        {
            ModelState.AddModelError(nameof(model.TableId), "Bàn đang có đơn mở. Vui lòng liên hệ thu ngân để gọi thêm món.");
            await LoadMenuAsync(model);
            return View("Menu", model);
        }
        var productIds = validCart.Select(x => x.ProductId).ToList();
        var products = await context.Products
            .Where(p => productIds.Contains(p.ProductID) && p.IsActive && !p.IsDeleted &&
                        p.Category != null && p.Category.IsActive && !p.Category.IsDeleted)
            .ToDictionaryAsync(p => p.ProductID);

        foreach (var item in validCart)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                ModelState.AddModelError(nameof(model.CartJson), $"Sản phẩm #{item.ProductId} không còn phục vụ.");
            else if (product.Quantity < item.Quantity)
                ModelState.AddModelError(nameof(model.CartJson),
                    $"{product.ProductName} chỉ còn {product.Quantity} sản phẩm.");
        }

        if (!ModelState.IsValid)
        {
            await LoadMenuAsync(model);
            return View("Menu", model);
        }

        var order = new Order
        {
            CustomerID = customer.CustomerID,
            TableID = table!.TableID,
            OrderStatus = OrderStatusConstants.Pending,
            OrderDate = DateTime.UtcNow,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in validCart)
        {
            var product = products[item.ProductId];
            product.Quantity -= item.Quantity;
            order.OrderDetails.Add(new OrderDetail
            {
                ProductID = product.ProductID,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                Subtotal = product.Price * item.Quantity,
                Notes = string.IsNullOrWhiteSpace(item.Notes) ? null : item.Notes.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        order.SubtotalAmount = order.OrderDetails.Sum(x => x.Subtotal);
        order.TotalAmount = order.SubtotalAmount;
        order.Payment = new Payment
        {
            Amount = order.TotalAmount,
            PaymentMethod = "Cash",
            PaymentStatus = PaymentStatusConstants.Pending,
            PaymentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        table.TableStatus = TableStatus.Occupied;
        table.UpdatedAt = DateTime.UtcNow;
        if (matchingReservation is not null && matchingReservation.ReservationStatus != "CheckedIn")
        {
            matchingReservation.ReservationStatus = "CheckedIn";
            matchingReservation.CheckinTime = DateTime.UtcNow;
            matchingReservation.UpdatedAt = DateTime.UtcNow;
        }

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        TempData["SuccessMessage"] =
             $"Đặt món thành công. Mã đơn #{order.OrderID}.";

        return RedirectToAction(nameof(Details), new { id = order.OrderID });
    }

    [HttpGet]
    public async Task<IActionResult> History(string? searchOrderId, string? searchTable, string? status)
    {
        if (!IsCustomer()) return RedirectToLogin();
        var customer = await _customerSessionService.GetOrCreateCustomerAsync();

        var query = context.Orders
            .Include(o => o.Table)
            .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.Product)
            .Where(o => !o.IsDeleted && o.CustomerID == customer.CustomerID)
            .AsQueryable();

        searchOrderId = searchOrderId?.Trim();
        if (!string.IsNullOrEmpty(searchOrderId))
        {
            var normalizedOrderId = searchOrderId.TrimStart('#');
            if (normalizedOrderId.StartsWith("ORD-", StringComparison.OrdinalIgnoreCase))
                normalizedOrderId = normalizedOrderId[4..];
            query = int.TryParse(normalizedOrderId, out var orderId)
                ? query.Where(o => o.OrderID == orderId)
                : query.Where(o => false);
        }

        searchTable = searchTable?.Trim();
        if (!string.IsNullOrEmpty(searchTable))
            query = query.Where(o => o.Table != null && o.Table.TableNumber.Contains(searchTable));

        var normalizedStatus = status?.Trim().ToLowerInvariant();
        var orderStatus = normalizedStatus switch
        {
            "completed" => OrderStatusConstants.Completed,
            "cancelled" => OrderStatusConstants.Cancelled,
            _ => null
        };
        if (normalizedStatus == "pending")
            query = query.Where(o => o.OrderStatus == OrderStatusConstants.Pending
                || o.OrderStatus == OrderStatusConstants.Preparing
                || o.OrderStatus == OrderStatusConstants.Ready
                || o.OrderStatus == OrderStatusConstants.WaitingPayment);
        else if (orderStatus is not null)
            query = query.Where(o => o.OrderStatus == orderStatus);

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders()
    {
        if (!IsCustomer()) return RedirectToLogin();
        var customer = await _customerSessionService.GetOrCreateCustomerAsync();

        var orders = await context.Orders
            .Include(o => o.Table)
            .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.Product)
            .Where(o => !o.IsDeleted && o.CustomerID == customer.CustomerID)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync();

        // Get reservations for this customer
        var reservations = await context.Reservations
            .Include(r => r.Table)
            .Where(r => !r.IsDeleted && r.CustomerID == customer.CustomerID)
            .OrderByDescending(r => r.ReservationDate)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Reservations = reservations;

        return View(orders);
    }
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsCustomer())
            return RedirectToLogin();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();

        var order = await context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Table)
            .Include(o => o.Payment)
            .Include(o => o.OrderDetails.Where(d => !d.IsDeleted))
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o =>
                !o.IsDeleted &&
                o.OrderID == id &&
                o.CustomerID == customer.CustomerID);

        if (order is null)
            return NotFound();

        var loyaltyQuote = await _loyaltyService.GetOrderQuoteAsync(
            order.OrderID,
            HttpContext.RequestAborted);
        var loyaltySummary = await _loyaltyService.GetCustomerSummaryAsync(
            customer.CustomerID,
            cancellationToken: HttpContext.RequestAborted);

        var vm = new Quản_lý_quán_cafe.Models.ViewModels.Order.OrderDetailViewModel
        {
            OrderId = order.OrderID,
            OrderCode = $"ORD-{order.OrderID:D6}",
            OrderDate = BusinessClock.FromUtc(order.OrderDate),
            OrderStatus = order.OrderStatus,
            CompletedDate = order.CompletedDate.HasValue
                ? BusinessClock.FromUtc(order.CompletedDate.Value) : null,
            Notes = order.Notes,

            CustomerId = order.CustomerID,
            CustomerName = order.Customer?.CustomerName,
            CustomerEmail = order.Customer?.Email,

            TableId = order.TableID,
            TableNumber = order.Table?.TableNumber?.ToString(),

            PaymentId = order.Payment?.PaymentID,
            PaymentStatus = order.Payment?.PaymentStatus,
            TotalAmount = loyaltyQuote.TotalAmount,
            PaidAmount = order.Payment?.PaymentStatus == PaymentStatusConstants.Completed
                ? order.Payment.Amount : 0,
            PaidDate = order.Payment?.PaymentStatus == PaymentStatusConstants.Completed
                ? BusinessClock.FromUtc(order.Payment.PaymentDate) : null,

            LoyaltySubtotalAmount = loyaltyQuote.SubtotalAmount,
            PointDiscountAmount = loyaltyQuote.PointDiscountAmount,
            VoucherDiscountAmount = loyaltyQuote.VoucherDiscountAmount,
            LoyaltyDiscountMode = loyaltyQuote.Mode,
            AppliedVoucherCode = loyaltyQuote.VoucherCode,
            AvailableRewardPoints = loyaltySummary.RewardPoints,
            AppliedRewardPoints = loyaltyQuote.Accounts.Sum(item => item.PointsUsed),
            ProjectedEarnedPoints = loyaltyQuote.EarnedPoints,
            CanApplyLoyaltyDiscount = order.Payment?.PaymentStatus != PaymentStatusConstants.Completed
                && order.OrderStatus != OrderStatusConstants.Completed
                && order.OrderStatus != OrderStatusConstants.Cancelled,

            Items = order.OrderDetails
                .Where(d => !d.IsDeleted)
                .Select(d => new Quản_lý_quán_cafe.Models.ViewModels.Order.OrderItemViewModel
                {
                    OrderDetailId = d.OrderDetailID,
                    ProductId = d.ProductID,
                    ProductName = d.Product?.ProductName ?? "Sản phẩm",
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    Notes = d.Notes
                })
                .ToList()
        };

        return View("Details", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyPoints(int id)
    {
        if (!IsCustomer()) return Unauthorized();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        var actorUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
        try
        {
            var quote = await _loyaltyService.ApplyPointsAsync(
                id,
                new[] { customer.CustomerID },
                actorUserId,
                ownerCustomerId: customer.CustomerID,
                cancellationToken: HttpContext.RequestAborted);
            return Ok(LoyaltyResponse(quote, "Đã áp dụng điểm thưởng."));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException
            or KeyNotFoundException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Customer {CustomerId} could not apply points to order {OrderId}.", customer.CustomerID, id);
            return LoyaltyError(ex);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyVoucher(int id, string? code)
    {
        if (!IsCustomer()) return Unauthorized();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        var actorUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
        try
        {
            var quote = await _loyaltyService.ApplyVoucherAsync(
                id,
                code?.Trim() ?? string.Empty,
                actorUserId,
                ownerCustomerId: customer.CustomerID,
                cancellationToken: HttpContext.RequestAborted);
            return Ok(LoyaltyResponse(quote, "Đã áp dụng voucher."));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException
            or KeyNotFoundException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Customer {CustomerId} could not apply a voucher to order {OrderId}.", customer.CustomerID, id);
            return LoyaltyError(ex);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearDiscount(int id)
    {
        if (!IsCustomer()) return Unauthorized();

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        var actorUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
        try
        {
            var quote = await _loyaltyService.ClearDiscountAsync(
                id,
                actorUserId,
                ownerCustomerId: customer.CustomerID,
                cancellationToken: HttpContext.RequestAborted);
            return Ok(LoyaltyResponse(quote, "Đã bỏ ưu đãi khỏi đơn hàng."));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException
            or KeyNotFoundException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Customer {CustomerId} could not clear the discount on order {OrderId}.", customer.CustomerID, id);
            return LoyaltyError(ex);
        }
    }

    private IActionResult LoyaltyError(Exception exception) => exception switch
    {
        LoyaltyRuleException loyaltyError => StatusCode(loyaltyError.StatusCode,
            new { success = false, message = loyaltyError.Message }),
        UnauthorizedAccessException => StatusCode(StatusCodes.Status403Forbidden,
            new { success = false, message = "Bạn không có quyền thay đổi ưu đãi của đơn hàng này." }),
        KeyNotFoundException => NotFound(new { success = false, message = "Không tìm thấy đơn hàng." }),
        _ => BadRequest(new { success = false, message = exception.Message })
    };

    private static object LoyaltyResponse(LoyaltyQuoteDto quote, string message) => new
    {
        success = true,
        message,
        quote.SubtotalAmount,
        quote.PointDiscountAmount,
        quote.VoucherDiscountAmount,
        quote.DiscountAmount,
        quote.TotalAmount,
        quote.Mode,
        quote.VoucherCode,
        quote.EarnedPoints,
        PointsUsed = quote.Accounts.Sum(item => item.PointsUsed)
    };
    private async Task LoadMenuAsync(OrderMenuViewModel model)
    {
        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
        var now = BusinessClock.Now;
        var latestStart = now.AddMinutes(ReservationPolicy.HoldBeforeMinutes);
        var oldestStart = now.AddMinutes(-ReservationPolicy.DurationMinutes);
        model.Tables = await context.RestaurantTables.AsNoTracking()
            .Where(t => !t.IsDeleted && t.TableStatus != TableStatus.Maintenance &&
                !t.Orders.Any(order => !order.IsDeleted && order.OrderStatus != OrderStatusConstants.Completed &&
                    order.OrderStatus != OrderStatusConstants.Cancelled &&
                    order.OrderDetails.Any(detail => !detail.IsDeleted)) &&
                (t.Reservations.Any(reservation => !reservation.IsDeleted &&
                    reservation.CustomerID == customer.CustomerID &&
                    (reservation.ReservationStatus == "Pending" || reservation.ReservationStatus == "Confirmed" ||
                     reservation.ReservationStatus == "CheckedIn") &&
                    reservation.ReservationDate <= latestStart && reservation.ReservationDate > oldestStart)
                 || (t.TableStatus == TableStatus.Available &&
                     !t.Reservations.Any(reservation => !reservation.IsDeleted &&
                         reservation.CustomerID != customer.CustomerID &&
                         reservation.ReservationStatus != "Cancelled" && reservation.ReservationStatus != "Completed" &&
                         reservation.ReservationDate <= latestStart && reservation.ReservationDate > oldestStart))))
            .OrderBy(t => t.TableNumber)
            .Select(t => new SelectListItem(
                $"{t.TableNumber} · {t.Location} · {t.TableStatus}",
                t.TableID.ToString(),
                t.TableID == model.TableId))
            .ToListAsync();

        model.Categories = await context.Categories.AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.CategoryName)
            .Select(c => new MenuCategoryViewModel
            {
                Name = c.CategoryName,
                Products = c.Products
                    .Where(p => !p.IsDeleted && p.IsActive && p.Quantity > 0)
                    .OrderBy(p => p.ProductName)
                    .Select(p => new MenuProductViewModel
                    {
                        ProductId = p.ProductID,
                        Name = p.ProductName,
                        Description = p.Description ?? string.Empty,
                        ImageUrl = p.ImageUrl,
                        Price = p.Price,
                        AvailableQuantity = p.Quantity
                    }).ToList()
            })
            .Where(c => c.Products.Count > 0)
            .ToListAsync();
    }

    private bool IsCustomer() =>
        (HttpContext.Session.GetInt32("UserId") ?? 0) > 0
        && HttpContext.Session.GetString("RoleName") == "Customer";

    private IActionResult RedirectToLogin() =>
        RedirectToAction("Login", "Account", new { area = "" });

    private async Task<Models.Entities.Customer> GetOrCreateCustomerAsync()
    {
        return await _customerSessionService.GetOrCreateCustomerAsync();
    }
}
