using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Areas.Customer.ViewModels;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Services;

namespace Quản_lý_quán_cafe.Areas.Customer.Controllers;

[Area("Customer")]
public class OrdersController(ApplicationDbContext context, Quản_lý_quán_cafe.Services.CustomerSessionService customerSessionService) : Controller
{
    private readonly Quản_lý_quán_cafe.Services.CustomerSessionService _customerSessionService = customerSessionService;
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

        cart = cart?
            .Where(x => x.ProductId > 0 && x.Quantity > 0)
            .GroupBy(x => x.ProductId)
            .Select(g => new CartItemInput(g.Key, g.Sum(x => x.Quantity), g.Last().Notes))
            .ToList();

        var table = await context.RestaurantTables
            .FirstOrDefaultAsync(t => t.TableID == model.TableId && !t.IsDeleted);
        if (table is null || table.TableStatus == TableStatus.Maintenance)
            ModelState.AddModelError(nameof(model.TableId), "Vui lòng chọn một bàn đang phục vụ.");
        if (cart is null || cart.Count == 0)
            ModelState.AddModelError(nameof(model.CartJson), "Giỏ hàng chưa có món.");

        if (!ModelState.IsValid)
        {
            await LoadMenuAsync(model);
            return View("Menu", model);
        }

        var validCart = cart!;
        await using var transaction = await context.Database.BeginTransactionAsync();
        var productIds = validCart.Select(x => x.ProductId).ToList();
        var products = await context.Products
            .Where(p => productIds.Contains(p.ProductID) && p.IsActive && !p.IsDeleted)
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
            await transaction.RollbackAsync();
            await LoadMenuAsync(model);
            return View("Menu", model);
        }

        var customer = await _customerSessionService.GetOrCreateCustomerAsync();
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

        order.TotalAmount = order.OrderDetails.Sum(x => x.Subtotal);
        table.TableStatus = TableStatus.Occupied;
        table.UpdatedAt = DateTime.UtcNow;

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        context.Payments.Add(new Payment
        {
            OrderID = order.OrderID,
            Amount = order.TotalAmount,
            PaymentMethod = "Cash",
            PaymentStatus = PaymentStatusConstants.Pending,
            PaymentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] =
             $"Đặt món thành công. Mã đơn #{order.OrderID}.";

        return RedirectToAction(nameof(Details), new { id = order.OrderID });
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        if (!IsCustomer()) return RedirectToLogin();
        var customer = await _customerSessionService.GetOrCreateCustomerAsync();

        var orders = await context.Orders
            .Include(o => o.Table)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .Where(o => !o.IsDeleted && o.CustomerID == customer.CustomerID)
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
            .Include(o => o.OrderDetails)
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

        var vm = new Quản_lý_quán_cafe.Models.ViewModels.Order.OrderDetailViewModel
        {
            OrderId = order.OrderID,
            OrderCode = $"ORD-{order.OrderID:D6}",
            OrderDate = order.OrderDate,
            OrderStatus = order.OrderStatus,
            CompletedDate = order.CompletedDate,
            Notes = order.Notes,

            CustomerId = order.CustomerID,
            CustomerName = order.Customer?.CustomerName,
            CustomerEmail = order.Customer?.Email,

            TableId = order.TableID,
            TableNumber = order.Table?.TableNumber?.ToString(),

            PaymentId = order.PaymentID,
            PaymentStatus = order.Payment?.PaymentStatus,
            TotalAmount = order.TotalAmount,
            PaidAmount = order.Payment?.Amount ?? 0,
            PaidDate = order.Payment?.PaymentDate,

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
    private async Task LoadMenuAsync(OrderMenuViewModel model)
    {
        model.Tables = await context.RestaurantTables.AsNoTracking()
            .Where(t => !t.IsDeleted && t.TableStatus != TableStatus.Maintenance)
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
