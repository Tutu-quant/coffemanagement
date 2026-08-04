using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Repository.Interfaces
{
    public interface IReservationRepository
    {
        Task<Reservation?> GetByIdAsync(int id);
        Task<List<Reservation>> GetAllAsync();
        Task<List<Reservation>> GetByCustomerAsync(int customerId);
        Task<List<Reservation>> GetByTableAsync(int tableId);
        Task<List<Reservation>> GetUpcomingAsync(int days = 7);
        Task<int> GetCountAsync();
        Task AddAsync(Reservation reservation);
        Task UpdateAsync(Reservation reservation);
        Task DeleteAsync(int id);

        /// <summary>
        /// Check if there is a reservation conflict for a table within a time range.
        /// Conflict means: overlap with any active reservation (not Cancelled/Completed).
        /// </summary>
        /// <param name="tableId">Table ID</param>
        /// <param name="reservationStart">Start time of proposed reservation</param>
        /// <param name="reservationEnd">End time of proposed reservation</param>
        /// <returns>True if there is a conflict, false otherwise</returns>
        Task<bool> HasConflictAsync(int tableId, DateTime reservationStart, DateTime reservationEnd);

        /// <summary>
        /// Get reservations for a specific table within a time range.
        /// Used to check availability.
        /// </summary>
        /// <param name="tableId">Table ID</param>
        /// <param name="reservationStart">Start time</param>
        /// <param name="reservationEnd">End time</param>
        /// <returns>List of conflicting reservations</returns>
        Task<List<Reservation>> GetConflictingReservationsAsync(
            int tableId,
            DateTime reservationStart,
            DateTime reservationEnd);

        Task<List<Reservation>> GetAvailableTablesReservationsAsync(
            int tableId,
            DateTime reservationDate,
            int durationMinutes = 120);

        Task<bool> GetReservationConflictAsync(
            int tableId,
            DateTime reservationDate,
            int durationMinutes = 120);

        Task UpdateStatusAsync(int reservationId, string newStatus);

        Task<List<Reservation>> GetReservationsByDateAsync(DateTime reservationDate);
    }
}
