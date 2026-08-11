using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository.Interfaces;

namespace Quản_lý_quán_cafe.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly ApplicationDbContext _context;

        public CustomerRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _context.Customers
                .Include(c => c.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == id && !c.IsDeleted);
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers
                .Include(c => c.User)
                .Where(c => !c.IsDeleted)
                .AsNoTracking()
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        public async Task<List<Customer>> SearchWithFilterAsync(string searchTerm, string sortBy, int skip, int take)
        {
            var query = _context.Customers
                .Include(c => c.User)
                .Where(c => !c.IsDeleted)
                .AsNoTracking();


            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CustomerName.Contains(searchTerm) ||
                                        (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                                        (c.Email != null && c.Email.Contains(searchTerm)) ||
                                        (c.User != null && c.User.Username.Contains(searchTerm)));
            }

            query = sortBy switch
            {
                "name_asc" => query.OrderBy(c => c.CustomerName),
                "name_desc" => query.OrderByDescending(c => c.CustomerName),
                "spent_desc" => query.OrderByDescending(c => c.TotalSpent),
                "points_desc" => query.OrderByDescending(c => c.RewardPoints),
                "newest" => query.OrderByDescending(c => c.CreatedAt),
                _ => query.OrderByDescending(c => c.CreatedAt)
            };

            return await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Customers
                .Where(c => !c.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetCountByFilterAsync(string searchTerm)
        {
            var query = _context.Customers
                .Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CustomerName.Contains(searchTerm) ||
                                        (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                                        (c.Email != null && c.Email.Contains(searchTerm)) ||
                                        (c.User != null && c.User.Username.Contains(searchTerm)));
            }

            return await query.CountAsync();
        }

        public async Task<int> GetCountTodayAsync()
        {
            var start = Models.BusinessClock.StartOfTodayUtc;
            var end = Models.BusinessClock.StartOfTomorrowUtc;
            return await _context.Customers
                .Where(c => !c.IsDeleted && c.CreatedAt >= start && c.CreatedAt < end)
                .CountAsync();
        }

        public async Task<bool> ExistsByPhoneAsync(string? phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            var normalizedPhone = phone.Trim();
            var query = _context.Customers
                .Where(c => !c.IsDeleted && c.Phone == normalizedPhone);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.CustomerID != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<Customer?> GetByEmailAsync(string email)
        {
            return await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == email && !c.IsDeleted);
        }

        public async Task<List<Order>> GetCustomerOrdersAsync(int customerId)
        {
            return await _context.Orders
                .Where(o => o.CustomerID == customerId && !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();
        }

        public async Task<decimal> GetCustomerTotalSpentAsync(int customerId)
        {
            return await _context.Customers
                .Where(c => c.CustomerID == customerId && !c.IsDeleted)
                .Select(c => c.TotalSpent)
                .FirstOrDefaultAsync();
        }

        public async Task AddAsync(Customer customer)
        {
            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            customer.UpdatedAt = DateTime.UtcNow;
            var entry = _context.Entry(customer);
            if (entry.State == EntityState.Detached)
                _context.Customers.Attach(customer);
            _context.Entry(customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var customer = await GetByIdAsync(id);
            if (customer != null)
            {
                customer.IsDeleted = true;
                await UpdateAsync(customer);
            }
        }
    }
}
