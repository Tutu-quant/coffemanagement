using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Models.Enums;
using Quản_lý_quán_cafe.Repository.Interfaces;

namespace Quản_lý_quán_cafe.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Existing Methods (Không thay đổi)

        public async Task<Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == id && !o.IsDeleted);
        }

        public async Task<Order?> GetByIdForUpdateAsync(int id)
        {
            return await _context.Orders
                // No AsNoTracking() - we want to track for updates
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderID == id && !o.IsDeleted);
        }

        public async Task<List<Order>> GetAllAsync()
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByCustomerAsync(int customerId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .Where(o => o.CustomerID == customerId && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetByTableAsync(int tableId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .Where(o => o.TableID == tableId && !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> SearchAsync(string searchTerm, int skip = 0, int take = 10)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .Where(o => !o.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(o => o.OrderID.ToString().Contains(searchTerm) || 
                                        (o.Notes != null && o.Notes.Contains(searchTerm)));
            }

            return await query
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Orders
                .Where(o => !o.IsDeleted)
                .CountAsync();
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
            var order = await GetByIdAsync(id);
            if (order != null)
            {
                order.IsDeleted = true;
                await UpdateAsync(order);
            }
        }

        #endregion

        #region New Methods for Admin Order Management

        /// <summary>
        /// Lấy danh sách đơn hàng theo trạng thái
        /// </summary>
        public async Task<List<Order>> GetByStatusAsync(string status, int skip = 0, int take = 50)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted && o.OrderStatus == status)
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Đếm số lượng đơn hàng theo trạng thái
        /// </summary>
        public async Task<int> GetCountByStatusAsync(string status)
        {
            return await _context.Orders
                .Where(o => !o.IsDeleted && o.OrderStatus == status)
                .CountAsync();
        }

        /// <summary>
        /// Lấy danh sách đơn hàng trong hôm nay
        /// </summary>
        public async Task<List<Order>> GetTodayOrdersAsync(int skip = 0, int take = 50)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted && 
                           o.OrderDate >= today && 
                           o.OrderDate < tomorrow)
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách đơn hàng gần đây nhất
        /// </summary>
        public async Task<List<Order>> GetRecentOrdersAsync(int take = 20)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.OrderDate)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách đơn hàng trong khoảng thời gian
        /// </summary>
        public async Task<List<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip = 0, int take = 50)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted && 
                           o.OrderDate >= startDate && 
                           o.OrderDate <= endDate)
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách đơn hàng có phân trang
        /// </summary>
        public async Task<(List<Order> Orders, int Total)> GetPagedOrdersAsync(int pageNumber = 1, int pageSize = 20)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted);

            var total = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, total);
        }

        /// <summary>
        /// Lấy thông tin tóm tắt đơn hàng (dùng cho dashboard)
        /// Tối ưu: Load toàn bộ data một lần, sau đó LINQ to Objects
        /// </summary>
        public async Task<dynamic> GetOrderSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var tomorrow = today.AddDays(1);

            // Load all data in one query
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted)
                .ToListAsync();

            // Process in-memory (LINQ to Objects)
            var summary = new
            {
                TotalOrders = orders.Count,
                TodayOrders = orders.Count(o => o.OrderDate >= today && o.OrderDate < tomorrow),
                PendingOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Pending),
                PreparingOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Preparing),
                ReadyOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Ready),
                WaitingPaymentOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.WaitingPayment),
                CompletedOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Completed),
                CancelledOrders = orders.Count(o => o.OrderStatus == OrderStatusConstants.Cancelled),
                TotalRevenue = orders.Where(o => o.OrderStatus == OrderStatusConstants.Completed).Sum(o => o.TotalAmount),
                TodayRevenue = orders
                    .Where(o => o.OrderStatus == OrderStatusConstants.Completed && 
                               o.OrderDate >= today && 
                               o.OrderDate < tomorrow)
                    .Sum(o => o.TotalAmount)
            };

            return summary;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng với lọc và tìm kiếm
        /// </summary>
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
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted);

            // Apply search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(o => 
                    o.OrderID.ToString().Contains(searchTerm) ||
                    (o.Customer != null && o.Customer.CustomerName.Contains(searchTerm)) ||
                    (o.Table != null && o.Table.TableNumber.Contains(searchTerm)) ||
                    (o.Notes != null && o.Notes.Contains(searchTerm)));
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            // Apply date range filter
            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endOfDay = endDate.Value.AddDays(1).AddSeconds(-1);
                query = query.Where(o => o.OrderDate <= endOfDay);
            }

            // Apply customer filter
            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(o => o.CustomerID == customerId.Value);
            }

            // Apply table filter
            if (tableId.HasValue && tableId.Value > 0)
            {
                query = query.Where(o => o.TableID == tableId.Value);
            }

            // Count total before paging
            var total = await query.CountAsync();

            // Apply sorting
            query = sortBy switch
            {
                SortingConstants.DateAsc => query.OrderBy(o => o.OrderDate),
                SortingConstants.AmountAsc => query.OrderBy(o => o.TotalAmount),
                SortingConstants.AmountDesc => query.OrderByDescending(o => o.TotalAmount),
                _ => query.OrderByDescending(o => o.OrderDate) // default: date_desc
            };

            // Apply pagination
            var orders = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orders, total);
        }

        /// <summary>
        /// Lấy danh sách đơn hàng theo nhiều trạng thái (dùng cho KDS)
        /// </summary>
        public async Task<List<Order>> GetByStatusesAsync(string[] statuses, int skip = 0, int take = 50)
        {
            if (statuses == null || statuses.Length == 0)
                return new List<Order>();

            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted && statuses.Contains(o.OrderStatus))
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách đơn hàng chưa thanh toán (dùng cho Payment)
        /// </summary>
        public async Task<List<Order>> GetUnpaidOrdersAsync(int skip = 0, int take = 50)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted && o.OrderStatus == OrderStatusConstants.WaitingPayment)
                .OrderByDescending(o => o.OrderDate)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách đơn hàng theo ID thanh toán (dùng cho Payment)
        /// </summary>
        public async Task<List<Order>> GetByPaymentIdAsync(int paymentId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Table)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted && o.PaymentID == paymentId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy doanh thu theo trạng thái (dùng cho Report)
        /// </summary>
        public async Task<dynamic> GetRevenueByStatusAsync()
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.OrderStatus == OrderStatusConstants.Completed)
                .ToListAsync();

            var revenue = new
            {
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                CompletedOrders = orders.Count,
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0m,
                HighestOrderValue = orders.Any() ? orders.Max(o => o.TotalAmount) : 0m,
                LowestOrderValue = orders.Any() ? orders.Min(o => o.TotalAmount) : 0m
            };

            return revenue;
        }

        /// <summary>
        /// Lấy doanh thu theo ngày (dùng cho Report/Dashboard)
        /// </summary>
        public async Task<dynamic> GetRevenueByDateAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .AsNoTracking()
                .Where(o => !o.IsDeleted && 
                           o.OrderStatus == OrderStatusConstants.Completed &&
                           o.OrderDate >= startDate && 
                           o.OrderDate <= endDate)
                .ToListAsync();

            // Group by date
            var revenueByDate = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    Orders = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToList();

            return new
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalRevenue = orders.Sum(o => o.TotalAmount),
                TotalOrders = orders.Count,
                AverageOrderValue = orders.Any() ? orders.Average(o => o.TotalAmount) : 0m,
                DailyBreakdown = revenueByDate
            };
        }

        #endregion
    }
}
