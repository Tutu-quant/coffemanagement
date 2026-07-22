using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Repository.Interfaces
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetByIdAsync(int id);
        Task<List<Customer>> GetAllAsync();
        Task<List<Customer>> SearchWithFilterAsync(string searchTerm, string? membershipTier, string sortBy, int skip, int take);
        Task<int> GetCountAsync();
        Task<int> GetCountByFilterAsync(string searchTerm, string? membershipTier);
        Task<int> GetCountVIPAsync();
        Task<int> GetCountTodayAsync();
        Task<long> GetTotalPointsAsync();
        Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null);
        Task<Customer?> GetByEmailAsync(string email);
        Task AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(int id);
        Task<List<Order>> GetCustomerOrdersAsync(int customerId);
        Task<decimal> GetCustomerTotalSpentAsync(int customerId);
    }
}
