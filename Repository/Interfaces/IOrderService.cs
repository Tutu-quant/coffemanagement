using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Services.Interfaces
{
    public interface IOrderService
    {
        #region Basic Operations

        Task<Order?> GetOrderByIdAsync(int id);

        Task<(List<Order> Orders, int Total)> GetOrdersAsync(
            int pageNumber = 1,
            int pageSize = 20);

        Task<List<Order>> GetOrdersByCustomerAsync(int customerId);

        Task<List<Order>> GetOrdersByTableAsync(int tableId);

        Task<(bool Success, string Message, Order? Order)> CreateOrderAsync(Order order);

        Task<(bool Success, string Message)> UpdateOrderAsync(Order order);

        Task<(bool Success, string Message)> DeleteOrderAsync(int id);

        #endregion

        #region Status Management

        Task<(bool Success, string Message)> UpdateOrderStatusAsync(int orderId, string status);

        Task<(List<Order> Orders, int Total)> GetOrdersByStatusAsync(
            string status,
            int pageNumber = 1,
            int pageSize = 20);

        Task<int> GetOrderCountByStatusAsync(string status);

        #endregion

        #region Search & Filter

        Task<(List<Order> Orders, int Total)> SearchOrdersAsync(
            string searchTerm,
            int pageNumber = 1,
            int pageSize = 20);

        Task<(List<Order> Orders, int Total)> FilterOrdersAsync(
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

        #region Timeline & Summary

        Task<List<Order>> GetRecentOrdersAsync(int take = 20);

        Task<List<Order>> GetTodayOrdersAsync(
            int pageNumber = 1,
            int pageSize = 20);

        #endregion

        #region Additional Methods

        Task<List<Order>> GetOrdersByMultipleStatusesAsync(
            string[] statuses,
            int pageNumber = 1,
            int pageSize = 20);

        Task<List<Order>> GetUnpaidOrdersAsync(
            int pageNumber = 1,
            int pageSize = 20);

        Task<List<Order>> GetOrdersByPaymentIdAsync(int paymentId);

        #endregion

        #region Kitchen Display

        /// <summary>
        /// Get orders for kitchen display (Pending, Preparing, Ready)
        /// </summary>
        Task<List<Order>> GetKitchenOrdersAsync();

        /// <summary>
        /// Start preparing an order (transition from Pending to Preparing)
        /// </summary>
        Task<(bool Success, string Message)> StartPreparingAsync(int orderId);

        /// <summary>
        /// Mark order as ready (transition from Preparing to Ready)
        /// </summary>
        Task<(bool Success, string Message)> MarkReadyAsync(int orderId);

        #endregion
    }
}
