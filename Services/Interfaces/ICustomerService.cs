using Quản_lý_quán_cafe.Models.ViewModels.Customer;

namespace Quản_lý_quán_cafe.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerDetailViewModel?> GetByIdAsync(int id);
        Task<CustomerListViewModel> GetAllAsync(int pageNumber = 1, int pageSize = 10);
        Task<CustomerListViewModel> SearchWithFilterAsync(string searchTerm, string? membershipTier, string sortBy, int pageNumber = 1, int pageSize = 10);
        Task<int> CreateAsync(CustomerCreateViewModel model);
        Task UpdateAsync(CustomerEditViewModel model);
        Task DeleteAsync(int id);
        Task<bool> ValidatePhoneAsync(string phone, int? excludeId = null);
        Task<CustomerStatisticsViewModel> GetStatisticsAsync();
    }
}
