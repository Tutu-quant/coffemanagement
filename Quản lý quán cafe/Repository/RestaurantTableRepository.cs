using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository.Interfaces;

namespace Quản_lý_quán_cafe.Repository
{
    public class RestaurantTableRepository : IRestaurantTableRepository
    {
        private readonly ApplicationDbContext _context;

        public RestaurantTableRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RestaurantTable?> GetByIdAsync(int id)
        {
            return await _context.RestaurantTables
                .FirstOrDefaultAsync(t => t.TableID == id && !t.IsDeleted);
        }

        public async Task<List<RestaurantTable>> GetAllAsync()
        {
            return await _context.RestaurantTables
                .Where(t => !t.IsDeleted)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();
        }

        public async Task<List<RestaurantTable>> SearchWithFilterAsync(string searchTerm, string? location, string? status, string sortBy, int skip = 0, int take = 10)
        {
            var query = _context.RestaurantTables
                .Where(t => !t.IsDeleted);

            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t => t.TableNumber.Contains(searchTerm) || 
                                        (t.Location != null && t.Location.Contains(searchTerm)));
            }

            // Location filter
            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(t => t.Location == location);
            }

            // Status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.TableStatus == status);
            }

            // Sorting
            query = sortBy switch
            {
                "name_asc" => query.OrderBy(t => t.TableNumber),
                "name_desc" => query.OrderByDescending(t => t.TableNumber),
                "capacity_asc" => query.OrderBy(t => t.Capacity),
                "capacity_desc" => query.OrderByDescending(t => t.Capacity),
                "date_newest" => query.OrderByDescending(t => t.CreatedAt),
                _ => query.OrderBy(t => t.TableNumber)
            };

            return await query
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.RestaurantTables
                .Where(t => !t.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetCountByFilterAsync(string searchTerm, string? location, string? status)
        {
            var query = _context.RestaurantTables
                .Where(t => !t.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(t => t.TableNumber.Contains(searchTerm) || 
                                        (t.Location != null && t.Location.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(t => t.Location == location);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.TableStatus == status);
            }

            return await query.CountAsync();
        }

        public async Task<int> GetCountByStatusAsync(string status)
        {
            return await _context.RestaurantTables
                .Where(t => !t.IsDeleted && t.TableStatus == status)
                .CountAsync();
        }

        public async Task<List<string>> GetAllLocationsAsync()
        {
            return await _context.RestaurantTables
                .Where(t => !t.IsDeleted && t.Location != null)
                .Select(t => t.Location!)
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();
        }

        public async Task<List<RestaurantTable>> GetTablesByStatusAsync(string status)
        {
            return await _context.RestaurantTables
                .Where(t => !t.IsDeleted && t.TableStatus == status)
                .OrderBy(t => t.TableNumber)
                .ToListAsync();
        }

        public async Task AddAsync(RestaurantTable table)
        {
            table.CreatedAt = DateTime.UtcNow;
            table.UpdatedAt = DateTime.UtcNow;
            await _context.RestaurantTables.AddAsync(table);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RestaurantTable table)
        {
            table.UpdatedAt = DateTime.UtcNow;
            _context.RestaurantTables.Update(table);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var table = await GetByIdAsync(id);
            if (table != null)
            {
                table.IsDeleted = true;
                await UpdateAsync(table);
            }
        }

        public async Task<bool> ExistsByTableNumberAsync(string tableNumber, int? excludeId = null)
        {
            var query = _context.RestaurantTables
                .Where(t => !t.IsDeleted && t.TableNumber == tableNumber);

            if (excludeId.HasValue)
            {
                query = query.Where(t => t.TableID != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<bool> CanDeleteAsync(int tableId)
        {
            var hasActiveOrders = await _context.Orders
                .AnyAsync(o => o.TableID == tableId && o.OrderStatus != "Completed" && o.OrderStatus != "Cancelled");

            var hasActiveReservations = await _context.Reservations
                .AnyAsync(r => r.TableID == tableId && r.ReservationStatus != "Completed" && r.ReservationStatus != "Cancelled");

            return !hasActiveOrders && !hasActiveReservations;
        }
    }
}
