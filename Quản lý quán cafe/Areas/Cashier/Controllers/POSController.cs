using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Areas.Cashier.ViewModels;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Models.ViewModels.Order;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    public class POSController : Controller
    {
        private readonly ApplicationDbContext _context;

        public POSController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index(int? tableId)
        {
            var tables = await _context.RestaurantTables
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();

            var activeOrders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => !o.IsDeleted && o.TableID != null &&
                    o.OrderStatus != OrderStatusConstants.Completed &&
                    o.OrderStatus != OrderStatusConstants.Cancelled)
                .ToListAsync();

            var selectedTableId = tableId ?? tables.FirstOrDefault()?.TableID ?? 0;
            var selectedOrder = activeOrders.FirstOrDefault(o => o.TableID == selectedTableId);
            var selectedTable = tables.FirstOrDefault(t => t.TableID == selectedTableId);

            var viewModel = new POSViewModel
            {
                OpenTables = tables.Select(table =>
                {
                    var order = activeOrders.FirstOrDefault(o => o.TableID == table.TableID);
                    return new POSTableViewModel
                    {
                        TableID = table.TableID,
                        TableNumber = table.TableNumber,
                        TableName = table.TableNumber,
                        OrderCode = order == null ? "Đơn mới" : $"#{order.OrderID}",
                        ItemCount = order?.OrderDetails.Sum(item => item.Quantity) ?? 0,
                        TotalAmount = order?.TotalAmount ?? 0,
                        Status = table.TableStatus.ToLowerInvariant(),
                        StatusBadge = table.TableStatus == "WaitingPayment" ? "THANH TOÁN" : string.Empty
                    };
                }).ToList(),
                AvailableProducts = await _context.Products
                    .Where(p => !p.IsDeleted && p.IsActive)
                    .OrderBy(p => p.ProductName)
                    .Select(p => new POSProductOptionViewModel
                    {
                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Price = p.Price
                    }).ToListAsync()
            };

            if (selectedTable != null)
            {
                viewModel.CurrentTable = viewModel.OpenTables.First(t => t.TableID == selectedTable.TableID);
                viewModel.Notes = selectedOrder?.Notes ?? string.Empty;

                if (selectedOrder != null)
                {
                    var order = await _context.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.OrderDetails).ThenInclude(item => item.Product)
                        .SingleAsync(o => o.OrderID == selectedOrder.OrderID);

                    viewModel.OrderItems = order.OrderDetails
                        .Where(item => !item.IsDeleted)
                        .Select(item => new POSOrderItemViewModel
                        {
                            OrderDetailID = item.OrderDetailID,
                            ProductID = item.ProductID,
                            ProductName = item.Product?.ProductName ?? "Món không xác định",
                            Price = item.UnitPrice,
                            Quantity = item.Quantity,
                            Notes = item.Notes ?? string.Empty
                        }).ToList();

                    if (order.Customer != null)
                    {
                        viewModel.Customer = new POSCustomerViewModel
                        {
                            CustomerID = order.Customer.CustomerID,
                            Name = order.Customer.CustomerName,
                            Phone = order.Customer.Phone ?? string.Empty,
                            RewardPoints = order.Customer.RewardPoints,
                            MembershipTier = order.Customer.MembershipTier
                        };
                    }
                }
            }

            viewModel.Subtotal = viewModel.OrderItems.Sum(item => item.Total);
            viewModel.Total = viewModel.Subtotal;
            return View(viewModel);
        }

        /// <summary>Tạo đơn hoặc thay thế danh sách món của đơn đang mở tại một bàn.</summary>
        [HttpPost]
        public async Task<IActionResult> SaveOrder([FromBody] CreateOrderViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = GetValidationMessage() });

            if (model.TableId is null)
                return BadRequest(new { success = false, message = "Vui lòng chọn bàn" });

            var table = await _context.RestaurantTables
                .SingleOrDefaultAsync(t => t.TableID == model.TableId && !t.IsDeleted);
            if (table == null)
                return NotFound(new { success = false, message = "Không tìm thấy bàn" });

            if (model.CustomerId is not null && !await _context.Customers
                    .AnyAsync(c => c.CustomerID == model.CustomerId && !c.IsDeleted))
                return BadRequest(new { success = false, message = "Không tìm thấy khách hàng" });

            var productIds = model.Items.Select(item => item.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(product => productIds.Contains(product.ProductID) && !product.IsDeleted && product.IsActive)
                .ToDictionaryAsync(product => product.ProductID);
            if (products.Count != productIds.Count)
                return BadRequest(new { success = false, message = "Một hoặc nhiều món không tồn tại hoặc đã ngừng bán" });

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .SingleOrDefaultAsync(o => o.TableID == model.TableId && !o.IsDeleted &&
                        o.OrderStatus != OrderStatusConstants.Completed &&
                        o.OrderStatus != OrderStatusConstants.Cancelled);

                var now = DateTime.UtcNow;
                if (order == null)
                {
                    order = new Order
                    {
                        TableID = model.TableId,
                        CustomerID = model.CustomerId,
                        OrderStatus = OrderStatusConstants.Pending,
                        OrderDate = now,
                        CreatedAt = now
                    };
                    _context.Orders.Add(order);
                }
                else
                {
                    _context.OrderDetails.RemoveRange(order.OrderDetails.Where(item => !item.IsDeleted));
                    order.CustomerID = model.CustomerId;
                    order.UpdatedAt = now;
                }

                order.Notes = model.Notes?.Trim();
                order.OrderDetails = model.Items.Select(item => new OrderDetail
                {
                    ProductID = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = products[item.ProductId].Price,
                    Subtotal = products[item.ProductId].Price * item.Quantity,
                    Notes = item.Notes?.Trim(),
                    CreatedAt = now
                }).ToList();
                order.TotalAmount = order.OrderDetails.Sum(item => item.Subtotal);
                table.TableStatus = "Occupied";
                table.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { success = true, orderId = order.OrderID, totalAmount = order.TotalAmount });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomer(string phone)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Phone == phone && !c.IsDeleted);
            if (customer == null) return Json(new { found = false });

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

        [HttpPost]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            var order = await _context.Orders
                .SingleOrDefaultAsync(o => o.TableID == request.TableId && !o.IsDeleted &&
                    o.OrderStatus != OrderStatusConstants.Completed && o.OrderStatus != OrderStatusConstants.Cancelled);
            if (order == null) return BadRequest(new { success = false, message = "Không có đơn hàng đang mở tại bàn này" });
            if (request.PaidAmount < order.TotalAmount) return BadRequest(new { success = false, message = "Số tiền khách đưa chưa đủ" });

            var now = DateTime.UtcNow;
            _context.Payments.Add(new Payment
            {
                OrderID = order.OrderID,
                Amount = order.TotalAmount,
                PaymentMethod = request.PaymentMethod?.Equals("qr", StringComparison.OrdinalIgnoreCase) == true ? "QR" : "Cash",
                PaymentStatus = PaymentStatusConstants.Completed,
                PaymentDate = now,
                CreatedAt = now
            });
            order.OrderStatus = OrderStatusConstants.Completed;
            order.CompletedDate = now;
            order.UpdatedAt = now;

            var table = await _context.RestaurantTables.FindAsync(request.TableId);
            if (table != null) { table.TableStatus = "Available"; table.UpdatedAt = now; }
            await _context.SaveChangesAsync();
            return Ok(new { success = true, changeAmount = request.PaidAmount - order.TotalAmount });
        }

        private string GetValidationMessage() => ModelState.Values.SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage).FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
            ?? "Dữ liệu đơn hàng không hợp lệ";

        public class CheckoutRequest
        {
            public int TableId { get; set; }
            public string? PaymentMethod { get; set; }
            public decimal PaidAmount { get; set; }
        }
    }
}
