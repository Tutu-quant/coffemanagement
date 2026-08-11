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
            var fromDate = Models.BusinessClock.Now;
            var toDate = fromDate.AddDays(days);

            return await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r => !r.IsDeleted &&
                           r.ReservationStatus != "Cancelled" && r.ReservationStatus != "Completed" &&
                           r.ReservationStatus != "CheckedIn" &&
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
            var entry = _context.Entry(reservation);
            if (entry.State == EntityState.Detached)
                _context.Reservations.Attach(reservation);
            _context.Entry(reservation).State = EntityState.Modified;
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

        public async Task<bool> HasConflictAsync(int tableId, DateTime reservationStart, DateTime reservationEnd)
        {
            return await _context.Reservations.AnyAsync(r =>
                r.TableID == tableId &&
                !r.IsDeleted &&
                r.ReservationStatus != "Cancelled" &&
                r.ReservationStatus != "Completed" &&
                r.ReservationDate < reservationEnd &&
                r.ReservationDate.AddHours(2) > reservationStart);
        }

        public async Task<List<Reservation>> GetConflictingReservationsAsync(
            int tableId,
            DateTime reservationStart,
            DateTime reservationEnd)
        {
            return await _context.Reservations
                .Where(r =>
                    r.TableID == tableId &&
                    !r.IsDeleted &&
                    r.ReservationStatus != "Cancelled" &&
                    r.ReservationStatus != "Completed" &&
                    r.ReservationDate < reservationEnd &&
                    r.ReservationDate.AddHours(2) > reservationStart)
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<List<Reservation>> GetAvailableTablesReservationsAsync(
            int tableId,
            DateTime reservationDate,
            int durationMinutes = 120)
        {
            var reservationEnd = reservationDate.AddMinutes(durationMinutes);

            return await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r =>
                    r.TableID == tableId &&
                    !r.IsDeleted &&
                    r.ReservationStatus != "Cancelled" &&
                    r.ReservationStatus != "Completed" &&
                    r.ReservationDate < reservationEnd &&
                    r.ReservationDate.AddHours(2) > reservationDate)
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<bool> GetReservationConflictAsync(
            int tableId,
            DateTime reservationDate,
            int durationMinutes = 120)
        {
            var reservationEnd = reservationDate.AddMinutes(durationMinutes);

            return await _context.Reservations.AnyAsync(r =>
                r.TableID == tableId &&
                !r.IsDeleted &&
                r.ReservationStatus != "Cancelled" &&
                r.ReservationStatus != "Completed" &&
                r.ReservationDate < reservationEnd &&
                r.ReservationDate.AddHours(2) > reservationDate);
        }

        public async Task UpdateStatusAsync(int reservationId, string newStatus)
        {
            var reservation = await GetByIdAsync(reservationId);
            if (reservation != null)
            {
                reservation.ReservationStatus = newStatus;
                reservation.UpdatedAt = DateTime.UtcNow;
                await UpdateAsync(reservation);
            }
        }

        public async Task<List<Reservation>> GetReservationsByDateAsync(DateTime reservationDate)
        {
            var startOfDay = reservationDate.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await _context.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Table)
                .Where(r =>
                    !r.IsDeleted &&
                    r.ReservationDate >= startOfDay &&
                    r.ReservationDate < endOfDay)
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();
        }
    }
}
