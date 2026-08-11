using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Models.ViewModels.Order;
using Quản_lý_quán_cafe.Services.Interfaces;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Models;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers
{
    [Area("Admin")]
    [SessionAuthorize("Admin")]
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _logger = logger;
        }

        public async Task<IActionResult> Print(int id, string? returnUrl = null)
        {
            if (id <= 0) return RedirectToAction(nameof(Index));
            try
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && returnUrl.StartsWith("/Cashier", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("Print", "Orders", new { area = "Cashier", id, returnUrl });
                }
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null) return RedirectToAction(nameof(Index));
                var viewModel = MapToOrderDetailViewModel(order);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not print admin order {OrderId}.", id);
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Index(
            string? keyword,
            string? status,
            string? paymentStatus,
            DateTime? dateFrom,
            DateTime? dateTo,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                pageNumber = Math.Clamp(pageNumber, 1, 1_000_000);
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var hasFilters = !string.IsNullOrWhiteSpace(keyword) || !string.IsNullOrWhiteSpace(status)
                    || !string.IsNullOrWhiteSpace(paymentStatus) || dateFrom.HasValue || dateTo.HasValue;
                DateTime? startUtc = dateFrom.HasValue ? BusinessClock.ToUtc(dateFrom.Value.Date) : null;
                DateTime? endUtc = dateTo.HasValue ? BusinessClock.ToUtc(dateTo.Value.Date.AddDays(1)) : null;
                var (orders, totalCount) = hasFilters
                    ? await _orderService.FilterOrdersAsync(keyword, status, paymentStatus, startUtc, endUtc,
                        pageNumber: pageNumber, pageSize: pageSize)
                    : await _orderService.GetOrdersAsync(pageNumber, pageSize);
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                if (totalPages > 0 && pageNumber > totalPages)
                    return RedirectToAction(nameof(Index), new
                    {
                        pageNumber = totalPages,
                        pageSize,
                        keyword,
                        status,
                        paymentStatus,
                        dateFrom = dateFrom?.ToString("yyyy-MM-dd"),
                        dateTo = dateTo?.ToString("yyyy-MM-dd")
                    });

                ViewBag.Keyword = keyword;
                ViewBag.Status = status;
                ViewBag.PaymentStatus = paymentStatus;
                ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
                ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
                var (_, todayCount) = await _orderService.FilterOrdersAsync(
                    startDate: BusinessClock.StartOfTodayUtc,
                    endDate: BusinessClock.StartOfTomorrowUtc,
                    pageNumber: 1,
                    pageSize: 1);
                ViewBag.TodayCount = todayCount;
                ViewBag.PreparingCount = await _orderService.GetOrderCountByStatusAsync(OrderStatusConstants.Preparing);
                ViewBag.ReadyCount = await _orderService.GetOrderCountByStatusAsync(OrderStatusConstants.Ready);
                ViewBag.WaitingPaymentCount = await _orderService.GetOrderCountByStatusAsync(OrderStatusConstants.WaitingPayment);
                ViewBag.CompletedCount = await _orderService.GetOrderCountByStatusAsync(OrderStatusConstants.Completed);

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
                _logger.LogError(ex, "Could not load admin order list.");
                TempData["Error"] = "Không thể tải danh sách đơn hàng. Vui lòng thử lại.";
                return View(new OrderListContainerViewModel());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] = "ID đơn hàng không hợp lệ";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = MapToOrderDetailViewModel(order);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not load admin order {OrderId}.", id);
                TempData["Error"] = "Không thể tải chi tiết đơn hàng. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Search(string? keyword, int pageNumber = 1, int pageSize = 20) =>
            RedirectToAction(nameof(Index), new { keyword, pageNumber, pageSize });

        public IActionResult Filter(
            string? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? keyword,
            int pageNumber = 1,
            int pageSize = 20)
            => RedirectToAction(nameof(Index), new { status, dateFrom, dateTo, keyword, pageNumber, pageSize });

        #region Helper Methods
        private List<OrderListViewModel> MapToOrderListViewModels(List<Models.Entities.Order> orders)
        {
            return orders.Select(o => new OrderListViewModel
            {
                OrderId = o.OrderID,
                OrderCode = $"#{o.OrderID:D6}",
                CustomerName = o.Customer?.CustomerName ?? "N/A",
                TableNumber = o.Table?.TableNumber ?? "N/A",
                EmployeeName = o.Employee?.FullName ?? "-",
                OrderStatus = o.OrderStatus ?? "Unknown",
                PaymentStatus = o.Payment?.PaymentStatus ?? "Pending",
                TotalAmount = o.TotalAmount,
                OrderDate = BusinessClock.FromUtc(o.OrderDate),
                ItemCount = o.OrderDetails?.Count ?? 0,
                StatusBadgeClass = GetStatusBadgeClass(o.OrderStatus)
            }).ToList();
        }
        private OrderDetailViewModel MapToOrderDetailViewModel(Models.Entities.Order order)
        {
            var viewModel = new OrderDetailViewModel
            {
                OrderId = order.OrderID,
                OrderCode = $"#{order.OrderID:D6}",
                OrderDate = BusinessClock.FromUtc(order.OrderDate),
                OrderStatus = order.OrderStatus ?? "Unknown",
                CompletedDate = order.CompletedDate.HasValue ? BusinessClock.FromUtc(order.CompletedDate.Value) : null,
                Notes = order.Notes,
                CustomerId = order.CustomerID,
                CustomerName = order.Customer?.CustomerName,
                CustomerPhone = order.Customer?.Phone,
                CustomerEmail = order.Customer?.Email,
                TableId = order.TableID,
                TableNumber = order.Table?.TableNumber,
                TableCapacity = order.Table?.Capacity,
                PaymentId = order.Payment?.PaymentID ?? order.PaymentID,
                PaymentStatus = order.Payment?.PaymentStatus ?? "Pending",
                TotalAmount = order.TotalAmount,
                PaidAmount = order.Payment?.PaymentStatus == "Completed" ? order.Payment.Amount : 0,
                PaidDate = order.Payment?.PaymentStatus == "Completed" ? BusinessClock.FromUtc(order.Payment.PaymentDate) : null,
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
                Items = order.OrderDetails?.Select(od => new OrderItemViewModel
                {
                    OrderDetailId = od.OrderDetailID,
                    ProductId = od.ProductID,
                    ProductName = od.Product?.ProductName ?? "Unknown",
                    Size = null,
                    UnitPrice = od.UnitPrice,
                    Quantity = od.Quantity,
                    Notes = od.Notes
                }).ToList() ?? new List<OrderItemViewModel>(),

                StatusBadgeClass = GetStatusBadgeClass(order.OrderStatus),
                Timeline = GenerateOrderTimeline(order)
            };

            return viewModel;
        }
        private string GetStatusBadgeClass(string? status)
        {
            return status switch
            {
                OrderStatusConstants.Pending => "badge-warning",
                OrderStatusConstants.Preparing => "badge-info",
                OrderStatusConstants.Ready => "badge-success",
                OrderStatusConstants.WaitingPayment => "badge-danger",
                OrderStatusConstants.Completed => "badge-success",
                OrderStatusConstants.Cancelled => "badge-secondary",
                _ => "badge-light"
            };
        }
        private List<OrderTimelineEventViewModel> GenerateOrderTimeline(Models.Entities.Order order)
        {
            var timeline = new List<OrderTimelineEventViewModel>
            {
                new OrderTimelineEventViewModel
                {
                    EventDate = BusinessClock.FromUtc(order.OrderDate),
                    EventType = "Created",
                    EventDescription = "Đơn hàng được tạo",
                    EventDetails = $"Order #{order.OrderID:D6}"
                }
            };

            if (order.OrderStatus == OrderStatusConstants.Completed && order.CompletedDate.HasValue)
            {
                timeline.Add(new OrderTimelineEventViewModel
                {
                    EventDate = BusinessClock.FromUtc(order.CompletedDate.Value),
                    EventType = "Completed",
                    EventDescription = "Đơn hàng hoàn thành",
                    EventDetails = $"Tổng tiền: {order.TotalAmount:N0}đ"
                });
            }

            if (order.Payment != null)
            {
                timeline.Add(new OrderTimelineEventViewModel
                {
                    EventDate = BusinessClock.FromUtc(order.Payment.PaymentDate),
                    EventType = "Payment",
                    EventDescription = "Thanh toán",
                    EventDetails = $"Trạng thái: {order.Payment.PaymentStatus}, Số tiền: {order.Payment.Amount:N0}đ"
                });
            }

            return timeline.OrderBy(e => e.EventDate).ToList();
        }
        private List<SelectListItem> GetStatusOptions(string? selectedStatus = null)
        {
            var statuses = new[]
            {
                OrderStatusConstants.Pending,
                OrderStatusConstants.Preparing,
                OrderStatusConstants.Ready,
                OrderStatusConstants.WaitingPayment,
                OrderStatusConstants.Completed,
                OrderStatusConstants.Cancelled
            };

            return statuses.Select(s => new SelectListItem
            {
                Value = s,
                Text = GetStatusDisplayName(s),
                Selected = s == selectedStatus
            }).ToList();
        }
        private string GetStatusDisplayName(string status)
        {
            return status switch
            {
                OrderStatusConstants.Pending => "Chờ xác nhận",
                OrderStatusConstants.Preparing => "Đang pha chế",
                OrderStatusConstants.Ready => "Sẵn sàng",
                OrderStatusConstants.WaitingPayment => "Chờ thanh toán",
                OrderStatusConstants.Completed => "Hoàn thành",
                OrderStatusConstants.Cancelled => "Hủy",
                _ => status
            };
        }

        #endregion
    }
}
