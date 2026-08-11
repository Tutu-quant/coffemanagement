using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Repository.Interfaces
{
    public interface IOrderRepository
    {
        #region CRUD

        Task<Order?> GetByIdAsync(int id);

        Task<Order?> GetByIdForUpdateAsync(int id);

        Task<List<Order>> GetAllAsync();

        Task AddAsync(Order order);

        Task UpdateAsync(Order order);

        Task DeleteAsync(int id);

        #endregion


        #region Query

        Task<List<Order>> GetByCustomerAsync(int customerId);

        Task<List<Order>> GetByTableAsync(int tableId);

        Task<List<Order>> GetByStatusAsync(
            string status,
            int skip = 0,
            int take = 50);


        Task<List<Order>> GetByStatusesAsync(
            string[] statuses,
            int skip = 0,
            int take = 50);

        Task<List<Order>> SearchAsync(
            string searchTerm,
            int skip = 0,
            int take = 10);

        Task<List<Order>> GetTodayOrdersAsync(
            int skip = 0,
            int take = 50);

        Task<List<Order>> GetRecentOrdersAsync(
            int take = 20);

        Task<List<Order>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            int skip = 0,
            int take = 50);

        #endregion


        #region Filter & Pagination

        Task<(List<Order> Orders, int Total)> GetPagedOrdersAsync(
            int pageNumber = 1,
            int pageSize = 20);

        Task<(List<Order> Orders, int Total)> GetFilteredOrdersAsync(
            string? searchTerm = null,
            string? status = null,
            string? paymentStatus = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            int? tableId = null,
            string sortBy = "date_desc",
            int pageNumber = 1,
            int pageSize = 20);

        #endregion


        #region Count

        Task<int> GetCountAsync();

        Task<int> GetCountByStatusAsync(string status);

        #endregion


        #region Payment

        Task<List<Order>> GetUnpaidOrdersAsync(int pageNumber = 1, int pageSize = 20);

        Task<List<Order>> GetByPaymentIdAsync(int paymentId);

        Task<dynamic> GetOrderSummaryAsync();

        Task<dynamic> GetOrderSummaryByDateRangeAsync(DateTime startDate, DateTime endDate);

        Task<dynamic> GetRevenueByStatusAsync();

        Task<dynamic> GetRevenueByDateAsync(DateTime startDate, DateTime endDate);

        #endregion
    }
}
