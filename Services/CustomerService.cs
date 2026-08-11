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
        private readonly IApplicationMutationCoordinator _mutationCoordinator;

        public CustomerService(
            ICustomerRepository repository,
            ApplicationDbContext context,
            IApplicationMutationCoordinator mutationCoordinator)
        {
            _repository = repository;
            _context = context;
            _mutationCoordinator = mutationCoordinator;
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
                Username = customer.User?.Username,
                RewardPoints = customer.RewardPoints,
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
                Username = customer.User?.Username,
                Address = customer.Address,
                RewardPoints = customer.RewardPoints,
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
            var customers = await _repository.SearchWithFilterAsync(string.Empty, "newest", skip, pageSize);

            var stats = await GetStatisticsAsync();

            return new CustomerListViewModel
            {
                Customers = customers.Select(MapToViewModel).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalCustomers = stats.TotalCustomers,
                CustomersToday = stats.CustomersToday,
                TotalPoints = stats.TotalPoints
            };
        }

        public async Task<CustomerListViewModel> SearchWithFilterAsync(string searchTerm, string sortBy, int pageNumber = 1, int pageSize = 10)
        {
            var totalItems = await _repository.GetCountByFilterAsync(searchTerm);
            var skip = (pageNumber - 1) * pageSize;
            var customers = await _repository.SearchWithFilterAsync(searchTerm, sortBy, skip, pageSize);

            var stats = await GetStatisticsAsync();

            return new CustomerListViewModel
            {
                Customers = customers.Select(MapToViewModel).ToList(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                SearchTerm = searchTerm,
                SortBy = sortBy,
                TotalCustomers = stats.TotalCustomers,
                CustomersToday = stats.CustomersToday,
                TotalPoints = stats.TotalPoints
            };
        }

        public async Task<int> CreateAsync(CustomerCreateViewModel model)
        {
            await using var mutationLock = await _mutationCoordinator.EnterAsync();
            var normalizedEmail = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLowerInvariant();
            if (normalizedEmail is not null && await _context.Customers.AnyAsync(customer => customer.Email == normalizedEmail))
                throw new InvalidOperationException("Email đã được sử dụng.");
            var customer = new Customer
            {
                CustomerName = model.Name.Trim(),
                Phone = model.Phone.Trim(),
                Email = normalizedEmail,
                Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim(),
                TotalSpent = model.TotalSpent,
                IsActive = model.IsActive
            };

            await _repository.AddAsync(customer);
            return customer.CustomerID;
        }

        public async Task UpdateAsync(CustomerEditViewModel model)
        {
            await using var mutationLock = await _mutationCoordinator.EnterAsync();
            var customer = await _context.Customers
                .Include(item => item.User)
                .FirstOrDefaultAsync(item => item.CustomerID == model.Id && !item.IsDeleted);
            if (customer != null)
            {
                var normalizedEmail = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim().ToLowerInvariant();
                if (normalizedEmail is not null && await _context.Customers.AnyAsync(item =>
                        item.CustomerID != model.Id && item.Email == normalizedEmail))
                    throw new InvalidOperationException("Email đã được sử dụng.");
                customer.CustomerName = model.Name.Trim();
                customer.Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();
                customer.Email = normalizedEmail;
                customer.Address = string.IsNullOrWhiteSpace(model.Address) ? null : model.Address.Trim();
                customer.TotalSpent = model.TotalSpent;
                customer.IsActive = model.IsActive;
                customer.UpdatedAt = DateTime.UtcNow;
                if (customer.User is not null)
                {
                    customer.User.IsActive = model.IsActive;
                    customer.User.UpdatedAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            await using var mutationLock = await _mutationCoordinator.EnterAsync();
            var hasOpenOrder = await _context.Orders.AnyAsync(order => order.CustomerID == id && !order.IsDeleted
                && order.OrderStatus != "Completed" && order.OrderStatus != "Cancelled"
                && order.OrderDetails.Any(detail => !detail.IsDeleted));
            if (hasOpenOrder)
                throw new InvalidOperationException("Không thể xóa khách hàng đang có đơn mở.");
            var hasActiveReservation = await _context.Reservations.AnyAsync(reservation => reservation.CustomerID == id
                && !reservation.IsDeleted && reservation.ReservationStatus != "Cancelled"
                && reservation.ReservationStatus != "Completed");
            if (hasActiveReservation)
                throw new InvalidOperationException("Không thể xóa khách hàng đang có lượt đặt bàn chưa hoàn thành.");

            var customer = await _context.Customers.Include(item => item.User)
                .FirstOrDefaultAsync(item => item.CustomerID == id && !item.IsDeleted);
            if (customer is null) return;
            customer.IsDeleted = true;
            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            if (customer.User is not null)
            {
                customer.User.IsActive = false;
                customer.User.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ValidatePhoneAsync(string? phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true;

            return !await _repository.ExistsByPhoneAsync(phone.Trim(), excludeId);
        }

        public async Task<CustomerStatisticsViewModel> GetStatisticsAsync()
        {
            var totalCustomers = await _repository.GetCountAsync();
            var customersToday = await _repository.GetCountTodayAsync();
            var totalPoints = await _context.Customers
                .Where(customer => !customer.IsDeleted)
                .SumAsync(customer => (long)customer.RewardPoints);

            return new CustomerStatisticsViewModel
            {
                TotalCustomers = totalCustomers,
                CustomersToday = customersToday,
                TotalPoints = totalPoints
            };
        }
    }
}
