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
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == id && !c.IsDeleted);
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _context.Customers
                .Where(c => !c.IsDeleted)
                .AsNoTracking()
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }

        public async Task<List<Customer>> SearchWithFilterAsync(string searchTerm, string? membershipTier, string sortBy, int skip, int take)
        {
            var query = _context.Customers
                .Where(c => !c.IsDeleted)
                .AsNoTracking();

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CustomerName.Contains(searchTerm) || 
                                        (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                                        (c.Email != null && c.Email.Contains(searchTerm)));
            }

            // Membership tier filter
            if (!string.IsNullOrEmpty(membershipTier))
            {
                query = query.Where(c => c.MembershipTier == membershipTier);
            }

            // Sort
            query = sortBy switch
            {
                "name_asc" => query.OrderBy(c => c.CustomerName),
                "name_desc" => query.OrderByDescending(c => c.CustomerName),
                "points_desc" => query.OrderByDescending(c => c.RewardPoints),
                "spent_desc" => query.OrderByDescending(c => c.TotalSpent),
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

        public async Task<int> GetCountByFilterAsync(string searchTerm, string? membershipTier)
        {
            var query = _context.Customers
                .Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.CustomerName.Contains(searchTerm) || 
                                        (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                                        (c.Email != null && c.Email.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(membershipTier))
            {
                query = query.Where(c => c.MembershipTier == membershipTier);
            }

            return await query.CountAsync();
        }

        public async Task<int> GetCountVIPAsync()
        {
            return await _context.Customers
                .Where(c => !c.IsDeleted && (c.MembershipTier == "VIP" || c.MembershipTier == "Diamond"))
                .CountAsync();
        }

        public async Task<int> GetCountTodayAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Customers
                .Where(c => !c.IsDeleted && c.CreatedAt.Date == today)
                .CountAsync();
        }

        public async Task<long> GetTotalPointsAsync()
        {
            return await _context.Customers
                .Where(c => !c.IsDeleted)
                .SumAsync(c => (long)c.RewardPoints);
        }

        public async Task<bool> ExistsByPhoneAsync(string phone, int? excludeId = null)
        {
            var query = _context.Customers
                .Where(c => !c.IsDeleted && c.Phone == phone);

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
            return await _context.Orders
                .Where(o => o.CustomerID == customerId && !o.IsDeleted)
                .SumAsync(o => o.TotalAmount);
        }

        public async Task AddAsync(Customer customer)
        {
            customer.CreatedAt = DateTime.UtcNow;
            customer.UpdatedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(customer.MembershipTier))
                customer.MembershipTier = "Member";
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            customer.UpdatedAt = DateTime.UtcNow;
            _context.Customers.Update(customer);
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
