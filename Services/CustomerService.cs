using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.ViewModels.Customer;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services.Interfaces;

namespace Quản_lý_quán_cafe.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repository;
        private readonly ApplicationDbContext _context;

        public CustomerService(ICustomerRepository repository, ApplicationDbContext context)
        {
            _repository = repository;
            _context = context;
        }

        private string GetAvatarInitials(string name)
        {
            var parts = name.Split(' ');
            if (parts.Length >= 2)
                return (parts[parts.Length - 2][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
            return parts[0].Length > 0 ? parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper() : "";
        }

        private CustomerViewModel MapToViewModel(Customer customer)
        {
            return new CustomerViewModel
            {
                Id = customer.CustomerID,
                Name = customer.CustomerName,
                Phone = customer.Phone,
                Email = customer.Email,
                RewardPoints = customer.RewardPoints,
                MembershipTier = customer.MembershipTier,
                TotalSpent = customer.TotalSpent,
                IsActive = customer.IsActive,
                LastVisit = customer.LastVisit,
                CreatedAt = customer.CreatedAt,
                AvatarInitials = GetAvatarInitials(customer.CustomerName)
            };
        }

        public async Task<CustomerDetailViewModel?> GetByIdAsync(int id)
        {
            var customer = await _repository.GetByIdAsync(id);
            if (customer == null) return null;

            var orders = await _repository.GetCustomerOrdersAsync(id);
            var totalSpent = await _repository.GetCustomerTotalSpentAsync(id);

            return new CustomerDetailViewModel
            {
                Id = customer.CustomerID,
                Name = customer.CustomerName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                RewardPoints = customer.RewardPoints,
                MembershipTier = customer.MembershipTier,
                TotalSpent = totalSpent,
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedAt,
                UpdatedAt = customer.UpdatedAt,
                AvatarInitials = GetAvatarInitials(customer.CustomerName),
                RecentOrders = orders.Select(o => new CustomerDetailViewModel.OrderDto
                {
                    Id = o.OrderID,
                    CreatedAt = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    Status = o.OrderStatus
                }).ToList()
            };
        }

        public async Task<CustomerListViewModel> GetAllAsync(int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountAsync();
            var skip = (pageNumber - 1) * pageSize;
            var customers = await _repository.SearchWithFilterAsync(string.Empty, null, "newest", skip, pageSize);

            var stats = await GetStatisticsAsync();

            return new CustomerListViewModel
            {
                Customers = customers.Select(MapToViewModel).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalCustomers = stats.TotalCustomers,
                VIPCustomers = stats.VIPCustomers,
                TotalPoints = stats.TotalPoints,
                CustomersToday = stats.CustomersToday
            };
        }

        public async Task<CustomerListViewModel> SearchWithFilterAsync(string searchTerm, string? membershipTier, string sortBy, int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountByFilterAsync(searchTerm, membershipTier);
            var skip = (pageNumber - 1) * pageSize;
            var customers = await _repository.SearchWithFilterAsync(searchTerm, membershipTier, sortBy, skip, pageSize);

            var stats = await GetStatisticsAsync();

            return new CustomerListViewModel
            {
                Customers = customers.Select(MapToViewModel).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                SearchTerm = searchTerm,
                SelectedMembershipTier = membershipTier,
                SortBy = sortBy,
                TotalCustomers = stats.TotalCustomers,
                VIPCustomers = stats.VIPCustomers,
                TotalPoints = stats.TotalPoints,
                CustomersToday = stats.CustomersToday
            };
        }

        public async Task<int> CreateAsync(CustomerCreateViewModel model)
        {
            var customer = new Customer
            {
                CustomerName = model.Name,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                RewardPoints = model.RewardPoints,
                MembershipTier = model.MembershipTier,
                TotalSpent = model.TotalSpent,
                IsActive = model.IsActive
            };

            await _repository.AddAsync(customer);
            return customer.CustomerID;
        }

        public async Task UpdateAsync(CustomerEditViewModel model)
        {
            var customer = await _repository.GetByIdAsync(model.Id);
            if (customer != null)
            {
                customer.CustomerName = model.Name;
                customer.Phone = model.Phone;
                customer.Email = model.Email;
                customer.Address = model.Address;
                customer.RewardPoints = model.RewardPoints;
                customer.MembershipTier = model.MembershipTier;
                customer.TotalSpent = model.TotalSpent;
                customer.IsActive = model.IsActive;

                await _repository.UpdateAsync(customer);
            }
        }

        public async Task DeleteAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<bool> ValidatePhoneAsync(string phone, int? excludeId = null)
        {
            return !await _repository.ExistsByPhoneAsync(phone, excludeId);
        }

        public async Task<CustomerStatisticsViewModel> GetStatisticsAsync()
        {
            var totalCustomers = await _repository.GetCountAsync();
            var vipCustomers = await _repository.GetCountAsync(); // VIP = Diamond + VIP tier
            var totalPoints = await _repository.GetTotalPointsAsync();
            var customersToday = await _repository.GetCountTodayAsync();

            // Tính VIP từ database
            var vipCount = await _context.Customers
                .Where(c => !c.IsDeleted && (c.MembershipTier == "VIP" || c.MembershipTier == "Diamond"))
                .CountAsync();

            return new CustomerStatisticsViewModel
            {
                TotalCustomers = totalCustomers,
                VIPCustomers = vipCount,
                TotalPoints = totalPoints,
                CustomersToday = customersToday
            };
        }
    }
}
