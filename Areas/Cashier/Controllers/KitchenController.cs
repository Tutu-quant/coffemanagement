using Microsoft.AspNetCore.Mvc;
using Quản_lý_quán_cafe.Areas.Cashier.ViewModels;
using Quản_lý_quán_cafe.Filters;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    [SessionAuthorize("Cashier,Admin")]
    public class KitchenController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<KitchenController> _logger;

        public KitchenController(IOrderService orderService, ILogger<KitchenController> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Display kitchen board with orders
        /// GET /Cashier/Kitchen
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch all kitchen orders (Pending, Preparing, Ready)
                var orders = await _orderService.GetKitchenOrdersAsync();

                // Count orders by status
                var pendingCount = await _orderService.GetOrderCountByStatusAsync(OrderStatusConstants.Pending);
                var preparingCount = await _orderService.GetOrderCountByStatusAsync(OrderStatusConstants.Preparing);
                var readyCount = await _orderService.GetOrderCountByStatusAsync(OrderStatusConstants.Ready);

                // Map to view models
                var kitchenOrders = orders.Select(o => MapToKitchenOrderViewModel(o)).ToList();

                var viewModel = new KitchenBoardViewModel
                {
                    PendingCount = pendingCount,
                    PreparingCount = preparingCount,
                    ReadyCount = readyCount,
                    Orders = kitchenOrders
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading kitchen board");
                return View(new KitchenBoardViewModel());
            }
        }

        /// <summary>
        /// Start preparing an order
        /// POST /Cashier/Kitchen/StartPreparing
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPreparing(int orderId)
        {
            if (orderId <= 0)
            {
                return Json(new { success = false, message = "ID đơn hàng không hợp lệ" });
            }

            var result = await _orderService.StartPreparingAsync(orderId);

            if (result.Success)
            {
                return Json(new { success = true, status = OrderStatusConstants.Preparing });
            }

            return Json(new { success = false, message = result.Message });
        }

        /// <summary>
        /// Mark order as ready
        /// POST /Cashier/Kitchen/MarkReady
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReady(int orderId)
        {
            if (orderId <= 0)
            {
                return Json(new { success = false, message = "ID đơn hàng không hợp lệ" });
            }

            var result = await _orderService.MarkReadyAsync(orderId);

            if (result.Success)
            {
                return Json(new { success = true, status = OrderStatusConstants.Ready });
            }

            return Json(new { success = false, message = result.Message });
        }

        /// <summary>
        /// Map Order entity to KitchenOrderViewModel
        /// </summary>
        private KitchenOrderViewModel MapToKitchenOrderViewModel(Models.Entities.Order order)
        {
            return new KitchenOrderViewModel
            {
                OrderId = order.OrderID,
                TableId = order.TableID,
                TableNumber = order.Table?.TableNumber,
                TableCapacity = order.Table?.Capacity,
                OrderDate = order.OrderDate,
                OrderStatus = order.OrderStatus,
                OrderNotes = order.Notes,
                Items = order.OrderDetails
                    .Where(od => !od.IsDeleted && od.Product != null)
                    .Select(od => new KitchenOrderItemViewModel
                    {
                        OrderDetailId = od.OrderDetailID,
                        ProductId = od.ProductID,
                        ProductName = od.Product?.ProductName ?? "Unknown",
                        Quantity = od.Quantity,
                        Notes = od.Notes
                    })
                    .ToList()
            };
        }
    }
}
