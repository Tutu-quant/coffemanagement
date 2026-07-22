using Microsoft.EntityFrameworkCore;
using Quản_lý_quán_cafe.Data;
using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository.Interfaces;

namespace Quản_lý_quán_cafe.Repository
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly ApplicationDbContext _context;

        public ReservationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Reservation?> GetByIdAsync(int id)
        {
            return await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .FirstOrDefaultAsync(r => r.ReservationID == id && !r.IsDeleted);
        }

        public async Task<List<Reservation>> GetAllAsync()
        {
            return await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetByCustomerAsync(int customerId)
        {
            return await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r => r.CustomerID == customerId && !r.IsDeleted)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetByTableAsync(int tableId)
        {
            return await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r => r.TableID == tableId && !r.IsDeleted)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetUpcomingAsync(int days = 7)
        {
            var fromDate = DateTime.UtcNow;
            var toDate = fromDate.AddDays(days);

            return await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r => !r.IsDeleted && 
                           r.ReservationDate >= fromDate && 
                           r.ReservationDate <= toDate)
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<int> GetCountAsync()
        {
            return await _context.Reservations
                .Where(r => !r.IsDeleted)
                .CountAsync();
        }

        public async Task AddAsync(Reservation reservation)
        {
            reservation.CreatedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;
            await _context.Reservations.AddAsync(reservation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Reservation reservation)
        {
            reservation.UpdatedAt = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var reservation = await GetByIdAsync(id);
            if (reservation != null)
            {
                reservation.IsDeleted = true;
                await UpdateAsync(reservation);
            }
        }
    }
}
