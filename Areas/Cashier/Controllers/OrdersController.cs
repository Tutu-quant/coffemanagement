using Microsoft.AspNetCore.Mvc;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    public class OrdersController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
<<<<<<< Updated upstream
            return View();
=======
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var (orders, totalCount) = await _orderService.GetOrdersAsync(pageNumber, pageSize);
                var viewModel = new OrderListContainerViewModel
                {
                    Orders = MapToOrderListViewModels(orders),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tải danh sách: {ex.Message}";
                return View(new OrderListContainerViewModel());
            }
        }

        private List<OrderListViewModel> MapToOrderListViewModels(List<Models.Entities.Order> orders)
        {
                return orders.Select(o => new OrderListViewModel
            {
                OrderId = o.OrderID,
                OrderCode = $"#{o.OrderID:D6}",
                CustomerName = o.Customer?.CustomerName ?? "N/A",
                TableNumber = o.Table?.TableNumber ?? "",
                EmployeeName = o.Employee?.FullName ?? "-",
                OrderStatus = o.OrderStatus ?? "Unknown",
                PaymentStatus = o.Payment?.PaymentStatus ?? "Pending",
                TotalAmount = o.TotalAmount,
                OrderDate = o.OrderDate,
                ItemCount = o.OrderDetails?.Count ?? 0,
                StatusBadgeClass = GetStatusBadgeClass(o.OrderStatus)
            }).ToList();
        }

        private string GetStatusBadgeClass(string? status)
        {
            return status switch
            {
                "Pending" => "bg-warning text-dark",
                "Preparing" => "bg-info text-white",
                "Ready" => "bg-primary text-white",
                "WaitingPayment" => "bg-secondary text-white",
                "Completed" => "bg-success text-white",
                _ => "bg-secondary text-white"
            };
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0) return RedirectToAction(nameof(Index));
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null) return RedirectToAction(nameof(Index));
                var viewModel = MapToOrderDetailViewModel(order);
                // provide a Cashier-specific PrintUrl so the shared Admin Details view will open the Cashier Print
                ViewData["PrintUrl"] = Url.Action("Print", "Orders", new { area = "Cashier", id, returnUrl = Url.Action("Details", "Orders", new { area = "Cashier", id }) });
                // reuse Admin details view for now (render cashier details using the shared admin view)
                return View("~/Areas/Admin/Views/Orders/Details.cshtml", viewModel);
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, string? returnUrl)
        {
            if (id <= 0) return RedirectToAction(nameof(Index));
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null) return RedirectToAction(nameof(Index));
                var viewModel = MapToOrderDetailViewModel(order);
                // prefer incoming returnUrl (from query) if provided, otherwise default to POS Index
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    ViewData["ReturnUrl"] = returnUrl;
                }
                else
                {
                    ViewData["ReturnUrl"] = Url.Action("Index", "POS", new { area = "Cashier" });
                }
                // use Cashier-specific print view
                return View("~/Areas/Cashier/Views/Orders/Print.cshtml", viewModel);
            }
            catch
            {
                return RedirectToAction(nameof(Index));
            }
        }

        private OrderDetailViewModel MapToOrderDetailViewModel(Models.Entities.Order order)
        {
            var viewModel = new OrderDetailViewModel
            {
                OrderId = order.OrderID,
                OrderCode = $"#{order.OrderID:D6}",
                OrderDate = order.OrderDate.ToLocalTime(),
                OrderStatus = order.OrderStatus ?? "Unknown",
                CompletedDate = order.CompletedDate?.ToLocalTime(),
                Notes = order.Notes,

                CustomerId = order.CustomerID,
                CustomerName = order.Customer?.CustomerName,
                CustomerPhone = order.Customer?.Phone,
                CustomerEmail = order.Customer?.Email,

                TableId = order.TableID,
                TableNumber = order.Table?.TableNumber,
                TableCapacity = order.Table?.Capacity,

                PaymentId = order.PaymentID,
                PaymentStatus = order.Payment?.PaymentStatus ?? "Pending",
                TotalAmount = order.TotalAmount,
                PaidAmount = order.Payment?.Amount ?? 0,
                PaidDate = order.Payment != null ? order.Payment.CreatedAt.ToLocalTime() : (DateTime?)null,

                Items = order.OrderDetails?.Select(od => new Models.ViewModels.Order.OrderItemViewModel
                {
                    OrderDetailId = od.OrderDetailID,
                    ProductId = od.ProductID,
                    ProductName = od.Product?.ProductName ?? "Unknown",
                    Size = null,
                    UnitPrice = od.UnitPrice,
                    Quantity = od.Quantity,
                    Notes = od.Notes
                }).ToList() ?? new List<Models.ViewModels.Order.OrderItemViewModel>(),

                StatusBadgeClass = GetStatusBadgeClass(order.OrderStatus),
                Timeline = GenerateOrderTimeline(order)
            };

            return viewModel;
        }

        private List<Models.ViewModels.Order.OrderTimelineEventViewModel> GenerateOrderTimeline(Models.Entities.Order order)
        {
            var list = new List<Models.ViewModels.Order.OrderTimelineEventViewModel>();
            list.Add(new Models.ViewModels.Order.OrderTimelineEventViewModel { EventDate = order.OrderDate.ToLocalTime(), EventType = "Created", EventDescription = "Order created" });
            if (order.CompletedDate.HasValue)
            {
                list.Add(new Models.ViewModels.Order.OrderTimelineEventViewModel { EventDate = order.CompletedDate.Value.ToLocalTime(), EventType = "Completed", EventDescription = "Order completed" });
            }
            return list;
>>>>>>> Stashed changes
        }
    }
}
