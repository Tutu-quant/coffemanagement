using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Services.Interfaces
{
    public interface IOrderService
    {
        #region Basic Operations

        /// <summary>Lấy đơn hàng theo ID kèm đầy đủ thông tin</summary>
        Task<Order?> GetOrderByIdAsync(int id);

        /// <summary>Lấy tất cả đơn hàng (phân trang)</summary>
        Task<(List<Order> Orders, int Total)> GetOrdersAsync(int pageNumber = 1, int pageSize = 20);

        /// <summary>Lấy danh sách đơn hàng của khách hàng</summary>
        Task<List<Order>> GetOrdersByCustomerAsync(int customerId);

        /// <summary>Lấy danh sách đơn hàng của bàn</summary>
        Task<List<Order>> GetOrdersByTableAsync(int tableId);

        /// <summary>Tạo đơn hàng mới</summary>
        Task<(bool Success, string Message, Order? Order)> CreateOrderAsync(Order order);

        /// <summary>Cập nhật thông tin đơn hàng</summary>
        Task<(bool Success, string Message)> UpdateOrderAsync(Order order);

        /// <summary>Xóa mềm đơn hàng</summary>
        Task<(bool Success, string Message)> DeleteOrderAsync(int id);

        #endregion

        #region Status Management

        /// <summary>Cập nhật trạng thái đơn hàng</summary>
        Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string status);

        /// <summary>Lấy danh sách đơn hàng theo trạng thái</summary>
        Task<(List<Order> Orders, int Total)> GetOrdersByStatusAsync(string status, int pageNumber = 1, int pageSize = 20);

        /// <summary>Đếm số lượng đơn hàng theo trạng thái</summary>
        Task<int> GetOrderCountByStatusAsync(string status);

        #endregion

        #region Search and Filter

        /// <summary>Tìm kiếm đơn hàng</summary>
        Task<(List<Order> Orders, int Total)> SearchOrdersAsync(string searchTerm, int pageNumber = 1, int pageSize = 20);

        /// <summary>Lọc đơn hàng với các điều kiện</summary>
        Task<(List<Order> Orders, int Total)> FilterOrdersAsync(
            string? searchTerm = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            int? tableId = null,
            string sortBy = "date_desc",
            int pageNumber = 1,
            int pageSize = 20);

        #endregion

        #region Timeline and Summary

        /// <summary>Lấy danh sách đơn hàng gần đây nhất</summary>
        Task<List<Order>> GetRecentOrdersAsync(int take = 20);

        /// <summary>Lấy danh sách đơn hàng trong hôm nay</summary>
        Task<List<Order>> GetTodayOrdersAsync(int pageNumber = 1, int pageSize = 20);

        /// <summary>Lấy thông tin tóm tắt đơn hàng (cho Dashboard)</summary>
        Task<dynamic> GetOrderSummaryAsync();

        /// <summary>Lấy thông tin tóm tắt đơn hàng theo thời gian</summary>
        Task<dynamic> GetOrderSummaryByDateRangeAsync(DateTime startDate, DateTime endDate);

        #endregion

        #region Additional Methods for Other Modules

        /// <summary>Lấy danh sách đơn hàng theo nhiều trạng thái (cho KDS)</summary>
        Task<List<Order>> GetOrdersByMultipleStatusesAsync(string[] statuses, int pageNumber = 1, int pageSize = 20);

        /// <summary>Lấy danh sách đơn hàng chưa thanh toán (cho Payment)</summary>
        Task<List<Order>> GetUnpaidOrdersAsync(int pageNumber = 1, int pageSize = 20);

        /// <summary>Lấy danh sách đơn hàng theo ID thanh toán (cho Payment)</summary>
        Task<List<Order>> GetOrdersByPaymentIdAsync(int paymentId);

        /// <summary>Lấy doanh thu theo trạng thái (cho Report)</summary>
        Task<dynamic> GetRevenueByStatusAsync();

        /// <summary>Lấy doanh thu theo ngày (cho Report/Dashboard)</summary>
        Task<dynamic> GetRevenueByDateAsync(DateTime startDate, DateTime endDate);

        #endregion
    }
}

