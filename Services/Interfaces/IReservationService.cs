using Quản_lý_quán_cafe.Models.Entities;

namespace Quản_lý_quán_cafe.Services.Interfaces
{
    /// <summary>
    /// Service layer for Reservation business logic.
    /// Handles all reservation operations, validations, and business rules.
    /// Does NOT access DbContext directly - uses Repository pattern.
    /// </summary>
    public interface IReservationService
    {
        /// <summary>
        /// Get available tables for a specific date and time range.
        /// </summary>
        /// <param name="reservationDate">The date and time for reservation</param>
        /// <param name="numberOfGuests">Number of guests (table capacity must be >= this)</param>
        /// <param name="durationMinutes">Reservation duration in minutes (default 120)</param>
        /// <returns>List of available tables that meet the criteria</returns>
        Task<List<RestaurantTable>> GetAvailableTablesAsync(
            DateTime reservationDate,
            int numberOfGuests,
            int durationMinutes = 120);

        /// <summary>
        /// Create a new reservation with full validation.
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="tableId">Table ID to reserve</param>
        /// <param name="reservationDate">Date and time of reservation</param>
        /// <param name="numberOfGuests">Number of guests</param>
        /// <param name="notes">Optional notes/requests</param>
        /// <returns>Created reservation object or null if validation failed</returns>
        Task<ReservationCreateResult> CreateReservationAsync(
            int customerId,
            int tableId,
            DateTime reservationDate,
            int numberOfGuests,
            string? notes = null);

        /// <summary>
        /// Cancel an existing reservation.
        /// </summary>
        /// <param name="reservationId">Reservation ID to cancel</param>
        /// <param name="customerId">Customer ID (for authorization check)</param>
        /// <returns>Result with status and message</returns>
        Task<ReservationOperationResult> CancelReservationAsync(
            int reservationId,
            int customerId);

        /// <summary>
        /// Confirm a pending reservation (Admin/Staff action).
        /// </summary>
        /// <param name="reservationId">Reservation ID to confirm</param>
        /// <returns>Result with status and message</returns>
        Task<ReservationOperationResult> ConfirmReservationAsync(int reservationId);

        /// <summary>
        /// Get all reservations for a specific customer.
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>List of reservations ordered by date descending</returns>
        Task<List<Reservation>> GetCustomerReservationsAsync(int customerId);

        /// <summary>
        /// Get detailed information about a specific reservation.
        /// </summary>
        /// <param name="reservationId">Reservation ID</param>
        /// <param name="customerId">Customer ID (for authorization check)</param>
        /// <returns>Reservation details or null if not found/unauthorized</returns>
        Task<Reservation?> GetReservationDetailsAsync(int reservationId, int customerId);

        /// <summary>
        /// Check if a table has any conflicts for the given time period.
        /// </summary>
        /// <param name="tableId">Table ID</param>
        /// <param name="reservationDate">Proposed reservation date/time</param>
        /// <param name="durationMinutes">Reservation duration in minutes</param>
        /// <returns>True if there is a conflict, false otherwise</returns>
        Task<bool> HasReservationConflictAsync(
            int tableId,
            DateTime reservationDate,
            int durationMinutes = 120);
    }

    /// <summary>
    /// Result of creating a reservation.
    /// </summary>
    public class ReservationCreateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Reservation? Reservation { get; set; }
        public string? ErrorCode { get; set; }

        public static ReservationCreateResult SuccessResult(Reservation reservation)
            => new()
            {
                Success = true,
                Message = "Đặt bàn thành công. Vui lòng chờ quán xác nhận.",
                Reservation = reservation
            };

        public static ReservationCreateResult FailureResult(string message, string? errorCode = null)
            => new()
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
    }

    /// <summary>
    /// Result of a reservation operation (cancel, confirm, etc).
    /// </summary>
    public class ReservationOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorCode { get; set; }

        public static ReservationOperationResult SuccessResult(string message)
            => new() { Success = true, Message = message };

        public static ReservationOperationResult FailureResult(string message, string? errorCode = null)
            => new() { Success = false, Message = message, ErrorCode = errorCode };
    }
}
