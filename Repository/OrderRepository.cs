using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Repository.Interfaces;

namespace Quản_lý_quán_cafe.Repository.Implementations
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region CRUD

       public async Task<List<Order>> GetByStatusesAsync(
            string[] statuses,
            int skip = 0,
            int take = 50)
        {
            if (statuses == null || statuses.Length == 0)
            {
                return new List<Order>();
            }

            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Where(o =>
                    !o.IsDeleted &&
                    statuses.Contains(o.OrderStatus))
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .Include(o => o.Table)
                .Include(o => o.Payment)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o =>
                    o.OrderID == id &&
                    !o.IsDeleted);
        }

        public async Task<Order?> GetByIdForUpdateAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .Include(o => o.Table)
                .Include(o => o.Payment)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o =>
                    o.OrderID == id &&
                    !o.IsDeleted);
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .Include(o => o.Table)
                .Include(o => o.Payment)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task AddAsync(Order order)
        {
            order.CreatedAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Order order)
        {
            order.UpdatedAt = DateTime.UtcNow;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var order = await GetByIdForUpdateAsync(id);

            if (order == null)
                throw new Exception("Không tìm thấy đơn hàng.");

            order.IsDeleted = true;
            order.UpdatedAt = DateTime.UtcNow;

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        #endregion


        #region Query Methods

        public async Task<List<Order>> GetByCustomerAsync(int customerId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Table)
                .Include(o => o.Employee)
                .Include(o => o.Payment)
                .Where(o => o.CustomerID == customerId && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByTableAsync(int tableId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .Include(o => o.Payment)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.TableID == tableId && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByStatusAsync(
            string status,
            int skip = 0,
            int take = 50)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Where(o => o.OrderStatus == status && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Order>> SearchAsync(
            string searchTerm,
            int skip = 0,
            int take = 10)
        {
            searchTerm = searchTerm.Trim();

            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.Employee)
                .Where(o =>
                    !o.IsDeleted &&
                    (
                        o.OrderID.ToString().Contains(searchTerm) ||
                        (o.Customer != null &&
                         o.Customer.CustomerName.Contains(searchTerm)) ||
                        (o.Table != null &&
                         o.Table.TableNumber.Contains(searchTerm)) ||
                        o.OrderStatus.Contains(searchTerm)
                    ))
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Order>> GetTodayOrdersAsync(
            int skip = 0,
            int take = 50)
        {
            var today = DateTime.Today;

            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Where(o =>
                    !o.IsDeleted &&
                    o.OrderDate.Date == today)
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Order>> GetRecentOrdersAsync(int take = 20)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.Employee)
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        #endregion


        #region Filter & Paging

        public async Task<List<Order>> GetByDateRangeAsync(
            DateTime startDate,
            DateTime endDate,
            int skip = 0,
            int take = 50)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .Include(o => o.Table)
                .Where(o =>
                    !o.IsDeleted &&
                    o.OrderDate >= startDate &&
                    o.OrderDate <= endDate)
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<(List<Order> Orders, int Total)> GetPagedOrdersAsync(
            int pageNumber = 1,
            int pageSize = 20)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .Include(o => o.Table)
                .Where(o => !o.IsDeleted);

            var total = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, total);
        }

        public async Task<(List<Order> Orders, int Total)> GetFilteredOrdersAsync(
            string? searchTerm = null,
            string? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? customerId = null,
            int? tableId = null,
            string sortBy = "date_desc",
            int pageNumber = 1,
            int pageSize = 20)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Employee)
                .Include(o => o.Table)
                .Where(o => !o.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(o =>
                    o.OrderID.ToString().Contains(searchTerm) ||
                    (o.Customer != null &&
                     o.Customer.CustomerName.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            if (customerId.HasValue)
            {
                query = query.Where(o => o.CustomerID == customerId);
            }

            if (tableId.HasValue)
            {
                query = query.Where(o => o.TableID == tableId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= endDate.Value);
            }

            query = sortBy switch
            {
                "date_asc" => query.OrderBy(o => o.OrderDate),
                "total_desc" => query.OrderByDescending(o => o.TotalAmount),
                "total_asc" => query.OrderBy(o => o.TotalAmount),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            var total = await query.CountAsync();

            var orders = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, total);
        }

        #endregion

        #region Count

        public async Task<int> GetCountAsync()
        {
            return await _context.Orders
                .CountAsync(o => !o.IsDeleted);
        }

        public async Task<int> GetCountByStatusAsync(string status)
        {
            return await _context.Orders
                .CountAsync(o =>
                    !o.IsDeleted &&
                    o.OrderStatus == status);
        }

        #endregion

        #region Payment

        public async Task<List<Order>> GetUnpaidOrdersAsync(
            int pageNumber = 1,
            int pageSize = 20)
        {
            var skip = (pageNumber - 1) * pageSize;
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Where(o =>
                    !o.IsDeleted &&
                    o.PaymentID == null)
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByPaymentIdAsync(int paymentId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Payment)
                .Where(o =>
                    !o.IsDeleted &&
                    o.PaymentID == paymentId)
                .ToListAsync();
        }

        public async Task<dynamic> GetOrderSummaryAsync()
        {
            var totalOrders = await _context.Orders.CountAsync(o => !o.IsDeleted);
            var completedOrders = await _context.Orders.CountAsync(o => !o.IsDeleted && o.OrderStatus == OrderStatusConstants.Completed);
            var pendingOrders = await _context.Orders.CountAsync(o => !o.IsDeleted && o.OrderStatus != OrderStatusConstants.Completed && o.OrderStatus != OrderStatusConstants.Cancelled);
            var totalRevenue = await _context.Orders.Where(o => !o.IsDeleted).SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            return new
            {
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue
            };
        }

        public async Task<dynamic> GetOrderSummaryByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderDate >= startDate && o.OrderDate <= endDate)
                .ToListAsync();

            return new
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalOrders = orders.Count,
                CompletedOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Completed),
                TotalRevenue = orders.Sum(o => o.TotalAmount)
            };
        }

        public async Task<dynamic> GetRevenueByStatusAsync()
        {
            var revenueByStatus = await _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted)
                .GroupBy(o => o.OrderStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();

            return new
            {
                RevenueByStatus = revenueByStatus
            };
        }

        public async Task<dynamic> GetRevenueByDateAsync(DateTime startDate, DateTime endDate)
        {
            var dailyRevenue = await _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderDate >= startDate && o.OrderDate <= endDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return new
            {
                StartDate = startDate,
                EndDate = endDate,
                DailyRevenue = dailyRevenue
            };
        }

        #endregion
    }
}