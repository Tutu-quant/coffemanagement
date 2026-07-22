using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Models.ViewModels.Order;
using Quản_lý_quán_cafe.Services.Interfaces;
using Quản_lý_quán_cafe.Models.Enums;

namespace Quản_lý_quán_cafe.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Authorize] // TODO: Implement custom authorization using Session
    public class OrdersController : Controller
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        }

        /// <summary>
        /// Danh sách đơn hàng với phân trang
        /// GET: /Admin/Orders
        /// </summary>
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 20)
        {
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

        /// <summary>
        /// Chi tiết đơn hàng
        /// GET: /Admin/Orders/Details/5
        /// </summary>
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
                TempData["Error"] = $"Lỗi khi tải chi tiết: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Tìm kiếm đơn hàng
        /// GET: /Admin/Orders/Search
        /// </summary>
        public async Task<IActionResult> Search(string? keyword, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var (orders, totalCount) = await _orderService.SearchOrdersAsync(keyword ?? string.Empty, pageNumber, pageSize);

                var viewModel = new OrderListContainerViewModel
                {
                    Orders = MapToOrderListViewModels(orders),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return View(nameof(Index), viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi tìm kiếm: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Lọc đơn hàng theo điều kiện
        /// GET: /Admin/Orders/Filter
        /// </summary>
        public async Task<IActionResult> Filter(
            string? status,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? keyword,
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var (orders, totalCount) = await _orderService.FilterOrdersAsync(
                    searchTerm: keyword,
                    status: status,
                    startDate: dateFrom,
                    endDate: dateTo,
                    pageNumber: pageNumber,
                    pageSize: pageSize);

                var viewModel = new OrderFilterViewModel
                {
                    Keyword = keyword,
                    Status = status,
                    DateFrom = dateFrom,
                    DateTo = dateTo,
                    Page = pageNumber,
                    PageSize = pageSize,
                    Results = MapToOrderListViewModels(orders),
                    TotalCount = totalCount,
                    StatusOptions = GetStatusOptions(status)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi lọc: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Helper Methods

        /// <summary>
        /// Map Order entity sang OrderListViewModel
        /// </summary>
        private List<OrderListViewModel> MapToOrderListViewModels(List<Models.Entities.Order> orders)
        {
            return orders.Select(o => new OrderListViewModel
            {
                OrderId = o.OrderID,
                OrderCode = $"#{o.OrderID:D6}",
                CustomerName = o.Customer?.CustomerName ?? "N/A",
                TableNumber = o.Table?.TableNumber ?? "N/A",
                EmployeeName = "N/A", // TODO: Add Employee info if needed
                OrderStatus = o.OrderStatus ?? "Unknown",
                PaymentStatus = o.Payment?.PaymentStatus ?? "Pending",
                TotalAmount = o.TotalAmount,
                OrderDate = o.OrderDate,
                ItemCount = o.OrderDetails?.Count ?? 0,
                StatusBadgeClass = GetStatusBadgeClass(o.OrderStatus)
            }).ToList();
        }

        /// <summary>
        /// Map Order entity sang OrderDetailViewModel
        /// </summary>
        private OrderDetailViewModel MapToOrderDetailViewModel(Models.Entities.Order order)
        {
            var viewModel = new OrderDetailViewModel
            {
                OrderId = order.OrderID,
                OrderCode = $"#{order.OrderID:D6}",
                OrderDate = order.OrderDate,
                OrderStatus = order.OrderStatus ?? "Unknown",
                CompletedDate = order.CompletedDate,
                Notes = order.Notes,

                // Customer info
                CustomerId = order.CustomerID,
                CustomerName = order.Customer?.CustomerName,
                CustomerPhone = order.Customer?.Phone,
                CustomerEmail = order.Customer?.Email,

                // Table info
                TableId = order.TableID,
                TableNumber = order.Table?.TableNumber,
                TableCapacity = order.Table?.Capacity,

                // Payment info
                PaymentId = order.PaymentID,
                PaymentStatus = order.Payment?.PaymentStatus ?? "Pending",
                TotalAmount = order.TotalAmount,
                PaidAmount = order.Payment?.Amount ?? 0,
                PaidDate = order.Payment?.CreatedAt,

                // Items
                Items = order.OrderDetails?.Select(od => new OrderItemViewModel
                {
                    OrderDetailId = od.OrderDetailID,
                    ProductId = od.ProductID,
                    ProductName = od.Product?.ProductName ?? "Unknown",
                    Size = null, // OrderDetail doesn't have Size property
                    UnitPrice = od.UnitPrice,
                    Quantity = od.Quantity,
                    Notes = od.Notes
                }).ToList() ?? new List<OrderItemViewModel>(),

                StatusBadgeClass = GetStatusBadgeClass(order.OrderStatus),
                Timeline = GenerateOrderTimeline(order)
            };

            return viewModel;
        }

        /// <summary>
        /// Lấy CSS class cho status badge
        /// </summary>
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

        /// <summary>
        /// Tạo danh sách sự kiện timeline cho đơn hàng
        /// </summary>
        private List<OrderTimelineEventViewModel> GenerateOrderTimeline(Models.Entities.Order order)
        {
            var timeline = new List<OrderTimelineEventViewModel>
            {
                new OrderTimelineEventViewModel
                {
                    EventDate = order.OrderDate,
                    EventType = "Created",
                    EventDescription = "Đơn hàng được tạo",
                    EventDetails = $"Order #{order.OrderID:D6}"
                }
            };

            if (order.OrderStatus == OrderStatusConstants.Completed && order.CompletedDate.HasValue)
            {
                timeline.Add(new OrderTimelineEventViewModel
                {
                    EventDate = order.CompletedDate.Value,
                    EventType = "Completed",
                    EventDescription = "Đơn hàng hoàn thành",
                    EventDetails = $"Tổng tiền: {order.TotalAmount:C}"
                });
            }

            if (order.Payment != null)
            {
                timeline.Add(new OrderTimelineEventViewModel
                {
                    EventDate = order.Payment.CreatedAt,
                    EventType = "Payment",
                    EventDescription = "Thanh toán",
                    EventDetails = $"Trạng thái: {order.Payment.PaymentStatus}, Số tiền: {order.Payment.Amount:C}"
                });
            }

            return timeline.OrderBy(e => e.EventDate).ToList();
        }

        /// <summary>
        /// Lấy danh sách tùy chọn trạng thái
        /// </summary>
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

        /// <summary>
        /// Lấy tên hiển thị của trạng thái
        /// </summary>
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
