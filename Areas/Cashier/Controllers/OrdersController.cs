using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Services.Interfaces;
using Quản_lý_quán_cafe.Models.ViewModels.Order;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Models;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    [SessionAuthorize("Cashier,Admin")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            IOrderService orderService,
            ILogger<OrdersController> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                pageNumber = Math.Clamp(pageNumber, 1, 1_000_000);
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var (orders, totalCount) = await _orderService.GetOrdersAsync(pageNumber, pageSize);
                var totalPages = Math.Max(1, (totalCount + pageSize - 1) / pageSize);
                if (pageNumber > totalPages)
                {
                    pageNumber = totalPages;
                    (orders, totalCount) = await _orderService.GetOrdersAsync(pageNumber, pageSize);
                }

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
                _logger.LogError(ex, "Không thể tải danh sách đơn hàng cho thu ngân");
                TempData["Error"] = "Không thể tải danh sách đơn hàng. Vui lòng thử lại.";
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
                OrderDate = ToLocalFromUtc(o.OrderDate),
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
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải chi tiết đơn hàng {OrderId} cho thu ngân", id);
                TempData["Error"] = "Không thể tải chi tiết đơn hàng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Print(int id, string? returnUrl = null)
        {
            if (id <= 0) return RedirectToAction(nameof(Index));
            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null) return RedirectToAction(nameof(Index));
                var viewModel = MapToOrderDetailViewModel(order);
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    ViewData["ReturnUrl"] = returnUrl;
                }
                else
                {
                    ViewData["ReturnUrl"] = null;
                }
                return View("~/Areas/Cashier/Views/Orders/Print.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tải bản in đơn hàng {OrderId} cho thu ngân", id);
                TempData["Error"] = "Không thể tải bản in đơn hàng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        private OrderDetailViewModel MapToOrderDetailViewModel(Models.Entities.Order order)
        {
            var viewModel = new OrderDetailViewModel
            {
                OrderId = order.OrderID,
                OrderCode = $"#{order.OrderID:D6}",
                OrderDate = ToLocalFromUtc(order.OrderDate),
                OrderStatus = order.OrderStatus ?? "Unknown",
                CompletedDate = order.CompletedDate.HasValue ? ToLocalFromUtc(order.CompletedDate.Value) : null,
                Notes = order.Notes,

                CustomerId = order.CustomerID,
                CustomerName = order.Customer?.CustomerName,
                CustomerPhone = order.Customer?.Phone,
                CustomerEmail = order.Customer?.Email,

                TableId = order.TableID,
                TableNumber = order.Table?.TableNumber,
                TableCapacity = order.Table?.Capacity,

                PaymentId = order.Payment?.PaymentID,
                PaymentStatus = order.Payment?.PaymentStatus ?? "Pending",
                TotalAmount = order.TotalAmount,
                PaidAmount = order.Payment?.PaymentStatus == "Completed" ? order.Payment.Amount : 0,
                PaidDate = order.Payment?.PaymentStatus == "Completed" ? ToLocalFromUtc(order.Payment.PaymentDate) : null,
                LoyaltySubtotalAmount = order.SubtotalAmount > 0
                    ? order.SubtotalAmount
                    : order.OrderDetails.Where(detail => !detail.IsDeleted).Sum(detail => detail.Subtotal),
                PointDiscountAmount = order.PointDiscountAmount,
                VoucherDiscountAmount = order.VoucherDiscountAmount,
                LoyaltyDiscountMode = order.VoucherDiscountAmount > 0
                    ? LoyaltyDiscountModes.Voucher
                    : order.PointDiscountAmount > 0 ? LoyaltyDiscountModes.Points : LoyaltyDiscountModes.None,
                AppliedVoucherCode = order.VoucherCode,
                AppliedRewardPoints = order.PointRedemptions.Sum(redemption => redemption.PointsUsed),
                ProjectedEarnedPoints = order.CustomerID.HasValue
                    ? (int)decimal.Floor((order.SubtotalAmount > 0
                        ? order.SubtotalAmount
                        : order.OrderDetails.Where(detail => !detail.IsDeleted).Sum(detail => detail.Subtotal))
                        / LoyaltyRules.VndPerEarnedPoint)
                    : 0,

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
            list.Add(new Models.ViewModels.Order.OrderTimelineEventViewModel
            {
                EventDate = ToLocalFromUtc(order.OrderDate),
                EventType = "Created",
                EventDescription = "Order created"
            });

            if (order.CompletedDate.HasValue)
            {
                list.Add(new Models.ViewModels.Order.OrderTimelineEventViewModel
                {
                    EventDate = ToLocalFromUtc(order.CompletedDate.Value),
                    EventType = "Completed",
                    EventDescription = "Order completed"
                });
            }

            return list;
        }

        private static DateTime ToLocalFromUtc(DateTime value) =>
            BusinessClock.FromUtc(value);
    }
}
