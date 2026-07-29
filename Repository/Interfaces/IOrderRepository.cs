using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Repository.Interfaces
{
    public interface IOrderRepository
    {
        #region Existing Methods

        /// <summary>Lấy đơn hàng theo ID</summary>
        Task<Order?> GetByIdAsync(int id);

        /// <summary>Lấy đơn hàng theo ID (tracked for update - không dùng AsNoTracking)</summary>
        Task<Order?> GetByIdForUpdateAsync(int id);

        /// <summary>Lấy tất cả đơn hàng</summary>
        Task<List<Order>> GetAllAsync();

        /// <summary>Lấy danh sách đơn hàng của khách hàng</summary>
        Task<List<Order>> GetByCustomerAsync(int customerId);

        /// <summary>Lấy danh sách đơn hàng của bàn</summary>
        Task<List<Order>> GetByTableAsync(int tableId);

        /// <summary>Tìm kiếm đơn hàng</summary>
        Task<List<Order>> SearchAsync(string searchTerm, int skip = 0, int take = 10);

        /// <summary>Đếm tổng số đơn hàng</summary>
        Task<int> GetCountAsync();

        /// <summary>Thêm mới đơn hàng</summary>
        Task AddAsync(Order order);

        /// <summary>Cập nhật đơn hàng</summary>
        Task UpdateAsync(Order order);

        /// <summary>Xóa mềm đơn hàng</summary>
        Task DeleteAsync(int id);

        #endregion

        #region New Methods for Admin Order Management

        /// <summary>Lấy danh sách đơn hàng theo trạng thái</summary>
        Task<List<Order>> GetByStatusAsync(string status, int skip = 0, int take = 50);

        /// <summary>Đếm số lượng đơn hàng theo trạng thái</summary>
        Task<int> GetCountByStatusAsync(string status);

        /// <summary>Lấy danh sách đơn hàng trong hôm nay</summary>
        Task<List<Order>> GetTodayOrdersAsync(int skip = 0, int take = 50);

        /// <summary>Lấy danh sách đơn hàng gần đây nhất</summary>
        Task<List<Order>> GetRecentOrdersAsync(int take = 20);

        /// <summary>Lấy danh sách đơn hàng trong khoảng thời gian</summary>
        Task<List<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int take = 50);

        /// <summary>Lấy danh sách đơn hàng có phân trang</summary>
        Task<(List<Order> Orders, int Total)> GetPagedOrdersAsync(int pageNumber = 1, int pageSize = 20);

        /// <summary>Lấy thông tin tóm tắt đơn hàng (dùng cho dashboard)</summary>
        Task<dynamic> GetOrderSummaryAsync();

        /// <summary>Lấy danh sách đơn hàng với lọc và tìm kiếm</summary>
        Task<(List<Order> Orders, int Total)> GetFilteredOrdersAsync(
            string? searchTerm = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            int? tableId = null,
            string sortBy = "date_desc",
            int pageNumber = 1,
            int pageSize = 20);

        /// <summary>Lấy danh sách đơn hàng theo nhiều trạng thái</summary>
        Task<List<Order>> GetByStatusesAsync(string[] statuses, int skip = 0, int take = 50);

        /// <summary>Lấy danh sách đơn hàng chưa thanh toán</summary>
        Task<List<Order>> GetUnpaidOrdersAsync(int skip = 0, int take = 50);

        /// <summary>Lấy danh sách đơn hàng theo ID thanh toán</summary>
        Task<List<Order>> GetByPaymentIdAsync(int paymentId);

        /// <summary>Lấy doanh thu theo trạng thái</summary>
        Task<dynamic> GetRevenueByStatusAsync();

        /// <summary>Lấy doanh thu theo ngày</summary>
        Task<dynamic> GetRevenueByDateAsync(DateTime startDate, DateTime endDate);

        #endregion
    }
}

