using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repository;

        public OrderService(IOrderRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        #region Basic Operations

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            if (id <= 0)
                return null;

            return await _repository.GetByIdAsync(id);
        }

        public async Task<(List<Order> Orders, int Total)> GetOrdersAsync(int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            return await _repository.GetPagedOrdersAsync(pageNumber, pageSize);
        }

        public async Task<List<Order>> GetOrdersByCustomerAsync(int customerId)
        {
            if (customerId <= 0)
                return new List<Order>();

            return await _repository.GetByCustomerAsync(customerId);
        }

        public async Task<List<Order>> GetOrdersByTableAsync(int tableId)
        {
            if (tableId <= 0)
                return new List<Order>();

            return await _repository.GetByTableAsync(tableId);
        }

        public async Task<(bool Success, string Message, Order? Order)> CreateOrderAsync(Order order)
        {
            try
            {
                if (order == null)
                    return (false, "Đơn hàng không hợp lệ", null);

                if (order.TotalAmount < 0)
                    return (false, "Tổng tiền không hợp lệ", null);

                // Set default status if not provided
                if (string.IsNullOrWhiteSpace(order.OrderStatus))
                    order.OrderStatus = OrderStatusConstants.Pending;

                order.OrderDate = DateTime.UtcNow;
                order.CreatedAt = DateTime.UtcNow;
                order.IsDeleted = false;

                await _repository.AddAsync(order);

                return (true, "Tạo đơn hàng thành công", order);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi tạo đơn hàng: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateOrderAsync(Order order)
        {
            try
            {
                if (order == null || order.OrderID <= 0)
                    return (false, "Đơn hàng không hợp lệ");

                var existingOrder = await _repository.GetByIdForUpdateAsync(order.OrderID);
                if (existingOrder == null)
                    return (false, "Không tìm thấy đơn hàng");

                // Update only allowed fields (don't change OrderDate, CreatedAt)
                existingOrder.Notes = order.Notes;
                existingOrder.TotalAmount = order.TotalAmount;
                existingOrder.UpdatedAt = DateTime.UtcNow;

                await _repository.UpdateAsync(existingOrder);

                return (true, "Cập nhật đơn hàng thành công");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật đơn hàng: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteOrderAsync(int id)
        {
            try
            {
                if (id <= 0)
                    return (false, "ID đơn hàng không hợp lệ");

                var order = await _repository.GetByIdForUpdateAsync(id);
                if (order == null)
                    return (false, "Không tìm thấy đơn hàng");

                await _repository.DeleteAsync(id);

                return (true, "Xóa đơn hàng thành công");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xóa đơn hàng: {ex.Message}");
            }
        }

        #endregion

        #region Status Management

        public async Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string status)
        {
            try
            {
                if (orderId <= 0)
                    return (false, "ID đơn hàng không hợp lệ");

                if (string.IsNullOrWhiteSpace(status))
                    return (false, "Trạng thái không hợp lệ");

                // Validate status
                var validStatuses = new[] 
                { 
                    OrderStatusConstants.Pending,
                    OrderStatusConstants.Preparing,
                    OrderStatusConstants.Ready,
                    OrderStatusConstants.WaitingPayment,
                    OrderStatusConstants.Completed,
                    OrderStatusConstants.Cancelled
                };

                if (!validStatuses.Contains(status))
                    return (false, "Trạng thái không được hỗ trợ");

                var order = await _repository.GetByIdForUpdateAsync(orderId);
                if (order == null)
                    return (false, "Không tìm thấy đơn hàng");

                order.OrderStatus = status;

                // Set CompletedDate if status is Completed
                if (status == OrderStatusConstants.Completed)
                    order.CompletedDate = DateTime.UtcNow;

                await _repository.UpdateAsync(order);

                return (true, "Cập nhật trạng thái thành công");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi cập nhật trạng thái: {ex.Message}");
            }
        }

        public async Task<(List<Order> Orders, int Total)> GetOrdersByStatusAsync(string status, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    return (new List<Order>(), 0);

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var skip = (pageNumber - 1) * pageSize;
                var orders = await _repository.GetByStatusAsync(status, skip, pageSize);
                var total = await _repository.GetCountByStatusAsync(status);

                return (orders, total);
            }
            catch
            {
                return (new List<Order>(), 0);
            }
        }

        public async Task<int> GetOrderCountByStatusAsync(string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status))
                    return 0;

                return await _repository.GetCountByStatusAsync(status);
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region Search and Filter

        public async Task<(List<Order> Orders, int Total)> SearchOrdersAsync(string searchTerm, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var skip = (pageNumber - 1) * pageSize;
                var orders = await _repository.SearchAsync(searchTerm ?? string.Empty, skip, pageSize);
                var total = await _repository.GetCountAsync();

                return (orders, total);
            }
            catch
            {
                return (new List<Order>(), 0);
            }
        }

        public async Task<(List<Order> Orders, int Total)> FilterOrdersAsync(
            string? searchTerm = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            int? tableId = null,
            string sortBy = "date_desc",
            int pageNumber = 1,
            int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                return await _repository.GetFilteredOrdersAsync(
                    searchTerm,
                    status,
                    startDate,
                    endDate,
                    customerId,
                    tableId,
                    sortBy,
                    pageNumber,
                    pageSize);
            }
            catch
            {
                return (new List<Order>(), 0);
            }
        }

        #endregion

        #region Timeline and Summary

        public async Task<List<Order>> GetRecentOrdersAsync(int take = 20)
        {
            try
            {
                if (take < 1 || take > 100) take = 20;
                return await _repository.GetRecentOrdersAsync(take);
            }
            catch
            {
                return new List<Order>();
            }
        }

        public async Task<List<Order>> GetTodayOrdersAsync(int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var skip = (pageNumber - 1) * pageSize;
                return await _repository.GetTodayOrdersAsync(skip, pageSize);
            }
            catch
            {
                return new List<Order>();
            }
        }

        public async Task<dynamic> GetOrderSummaryAsync()
        {
            try
            {
                return await _repository.GetOrderSummaryAsync();
            }
            catch
            {
                return new
                {
                    TotalOrders = 0,
                    TodayOrders = 0,
                    PendingOrders = 0,
                    PreparingOrders = 0,
                    ReadyOrders = 0,
                    WaitingPaymentOrders = 0,
                    CompletedOrders = 0,
                    CancelledOrders = 0,
                    TotalRevenue = 0m,
                    TodayRevenue = 0m
                };
            }
        }

        public async Task<dynamic> GetOrderSummaryByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (endDate < startDate)
                    endDate = startDate.AddDays(1);

                var orders = await _repository.GetByDateRangeAsync(startDate, endDate, 0, int.MaxValue);

                var summary = new
                {
                    TotalOrders = orders.Count,
                    PendingOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Pending),
                    PreparingOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Preparing),
                    ReadyOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Ready),
                    WaitingPaymentOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.WaitingPayment),
                    CompletedOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Completed),
                    CancelledOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Cancelled),
                    TotalRevenue = orders.Where(o => o.OrderStatus == OrderStatusConstants.Completed).Sum(o => o.TotalAmount)
                };

                return summary;
            }
            catch
            {
                return new
                {
                    TotalOrders = 0,
                    PendingOrders = 0,
                    PreparingOrders = 0,
                    ReadyOrders = 0,
                    WaitingPaymentOrders = 0,
                    CompletedOrders = 0,
                    CancelledOrders = 0,
                    TotalRevenue = 0m
                };
            }
        }

        #endregion

        #region Additional Methods for Other Modules

        public async Task<List<Order>> GetOrdersByMultipleStatusesAsync(string[] statuses, int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (statuses == null || statuses.Length == 0)
                    return new List<Order>();

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var skip = (pageNumber - 1) * pageSize;
                return await _repository.GetByStatusesAsync(statuses, skip, pageSize);
            }
            catch
            {
                return new List<Order>();
            }
        }

        public async Task<List<Order>> GetUnpaidOrdersAsync(int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var skip = (pageNumber - 1) * pageSize;
                return await _repository.GetUnpaidOrdersAsync(skip, pageSize);
            }
            catch
            {
                return new List<Order>();
            }
        }

        public async Task<List<Order>> GetOrdersByPaymentIdAsync(int paymentId)
        {
            try
            {
                if (paymentId <= 0)
                    return new List<Order>();

                return await _repository.GetByPaymentIdAsync(paymentId);
            }
            catch
            {
                return new List<Order>();
            }
        }

        public async Task<dynamic> GetRevenueByStatusAsync()
        {
            try
            {
                return await _repository.GetRevenueByStatusAsync();
            }
            catch
            {
                return new
                {
                    TotalRevenue = 0m,
                    CompletedOrders = 0,
                    AverageOrderValue = 0m,
                    HighestOrderValue = 0m,
                    LowestOrderValue = 0m
                };
            }
        }

        public async Task<dynamic> GetRevenueByDateAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (endDate < startDate)
                    endDate = startDate.AddDays(1);

                return await _repository.GetRevenueByDateAsync(startDate, endDate);
            }
            catch
            {
                return new
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    TotalRevenue = 0m,
                    TotalOrders = 0,
                    AverageOrderValue = 0m,
                    DailyBreakdown = new List<dynamic>()
                };
            }
        }

        #endregion
    }
}
