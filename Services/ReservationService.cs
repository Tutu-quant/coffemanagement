using Quản_lý_quán_cafe.Models.Entities;
using Quản_lý_quán_cafe.Repository.Interfaces;
using Quản_lý_quán_cafe.Services.Interfaces;
using Quản_lý_quán_cafe.Models;

namespace Quản_lý_quán_cafe.Services
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly IRestaurantTableRepository _tableRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IApplicationMutationCoordinator _mutationCoordinator;

        private const int ReservationDurationMinutes = ReservationPolicy.DurationMinutes;
        private const int BufferMinutesBefore = 0;
        private const int BufferMinutesAfter = 0;

        public ReservationService(
            IReservationRepository reservationRepository,
            IRestaurantTableRepository tableRepository,
            ICustomerRepository customerRepository,
            IApplicationMutationCoordinator mutationCoordinator)
        {
            _reservationRepository = reservationRepository;
            _tableRepository = tableRepository;
            _customerRepository = customerRepository;
            _mutationCoordinator = mutationCoordinator;
        }

        public async Task<List<RestaurantTable>> GetAvailableTablesAsync(
            DateTime reservationDate,
            int numberOfGuests,
            int durationMinutes = 120)
        {
            if (numberOfGuests <= 0)
                return new List<RestaurantTable>();

            if (reservationDate <= BusinessClock.Now)
                return new List<RestaurantTable>();

            var allTables = await _tableRepository.GetAvailableTablesAsync(numberOfGuests);
            var currentUseCutoff = BusinessClock.Now.AddMinutes(ReservationPolicy.DurationMinutes);
            if (reservationDate <= currentUseCutoff)
                allTables = allTables.Where(t => t.TableStatus is not ("Occupied" or "WaitingPayment")).ToList();

            var availableTables = new List<RestaurantTable>();

            foreach (var table in allTables)
            {
                var hasConflict = await _reservationRepository.HasConflictAsync(
                    table.TableID,
                    reservationDate,
                    reservationDate.AddMinutes(durationMinutes));

                if (!hasConflict)
                {
                    availableTables.Add(table);
                }
            }

            return availableTables.OrderBy(t => t.TableNumber).ToList();
        }

        public async Task<ReservationCreateResult> CreateReservationAsync(
            int customerId,
            int tableId,
            DateTime reservationDate,
            int numberOfGuests,
            string? notes = null)
        {
            if (customerId <= 0)
                return ReservationCreateResult.FailureResult("Khách hàng không hợp lệ.", "INVALID_CUSTOMER");

            if (tableId <= 0)
                return ReservationCreateResult.FailureResult("Bàn không hợp lệ.", "INVALID_TABLE");

            if (numberOfGuests <= 0 || numberOfGuests > 50)
                return ReservationCreateResult.FailureResult("Số khách phải từ 1 đến 50.", "INVALID_GUEST_COUNT");

            notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            if (notes?.Length > 500)
                return ReservationCreateResult.FailureResult("Ghi chú không được vượt quá 500 ký tự.", "INVALID_NOTES");

            if (reservationDate <= BusinessClock.Now)
                return ReservationCreateResult.FailureResult("Thời gian đặt phải ở tương lai.", "INVALID_DATE");

            await using var mutationLock = await _mutationCoordinator.EnterAsync();

            var customer = await _customerRepository.GetByIdAsync(customerId);
            if (customer == null || customer.IsDeleted)
                return ReservationCreateResult.FailureResult("Khách hàng không tồn tại.", "CUSTOMER_NOT_FOUND");

            var table = await _tableRepository.GetByIdAsync(tableId);
            if (table == null || table.IsDeleted)
                return ReservationCreateResult.FailureResult("Bàn không tồn tại.", "TABLE_NOT_FOUND");

            if (table.TableStatus == "Maintenance")
                return ReservationCreateResult.FailureResult("Bàn đang bảo trì.", "TABLE_MAINTENANCE");

            if (reservationDate <= BusinessClock.Now.AddMinutes(ReservationPolicy.DurationMinutes) &&
                (table.TableStatus is "Occupied" or "WaitingPayment"))
                return ReservationCreateResult.FailureResult("Bàn đang được sử dụng hoặc chờ thanh toán.", "TABLE_IN_USE");

            if (numberOfGuests > table.Capacity)
                return ReservationCreateResult.FailureResult(
                    $"Bàn {table.TableNumber} chỉ phục vụ tối đa {table.Capacity} khách.",
                    "INSUFFICIENT_CAPACITY");

            var reservationStart = reservationDate;
            var reservationEnd = reservationDate.AddMinutes(ReservationDurationMinutes);

            var hasConflict = await _reservationRepository.HasConflictAsync(tableId, reservationStart, reservationEnd);
            if (hasConflict)
                return ReservationCreateResult.FailureResult(
                    "Bàn đã được đặt gần khung giờ này.",
                    "RESERVATION_CONFLICT");

            var reservation = new Reservation
            {
                CustomerID = customerId,
                TableID = tableId,
                ReservationDate = reservationDate,
                ReservationTime = reservationDate,
                NumberOfGuests = numberOfGuests,
                ReservationStatus = "Pending",
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _reservationRepository.AddAsync(reservation);

            return ReservationCreateResult.SuccessResult(reservation);
        }

        public async Task<ReservationOperationResult> CancelReservationAsync(
            int reservationId,
            int customerId)
        {
            if (reservationId <= 0)
                return ReservationOperationResult.FailureResult("Mã đặt bàn không hợp lệ.", "INVALID_RESERVATION");

            await using var mutationLock = await _mutationCoordinator.EnterAsync();
            var reservation = await _reservationRepository.GetByIdAsync(reservationId);
            if (reservation == null)
                return ReservationOperationResult.FailureResult("Đặt bàn không tồn tại.", "RESERVATION_NOT_FOUND");

            if (reservation.CustomerID != customerId)
                return ReservationOperationResult.FailureResult("Bạn không có quyền hủy đặt bàn này.", "UNAUTHORIZED");

            if (reservation.ReservationStatus is "Cancelled" or "Completed" or "CheckedIn")
                return ReservationOperationResult.FailureResult(
                    "Không thể hủy đặt bàn ở trạng thái hiện tại.",
                    "INVALID_STATUS");

            reservation.ReservationStatus = "Cancelled";
            reservation.UpdatedAt = DateTime.UtcNow;

            await _reservationRepository.UpdateAsync(reservation);

            return ReservationOperationResult.SuccessResult("Đã hủy đặt bàn thành công.");
        }

        public async Task<ReservationOperationResult> ConfirmReservationAsync(int reservationId)
        {
            if (reservationId <= 0)
                return ReservationOperationResult.FailureResult("Mã đặt bàn không hợp lệ.", "INVALID_RESERVATION");

            await using var mutationLock = await _mutationCoordinator.EnterAsync();
            var reservation = await _reservationRepository.GetByIdAsync(reservationId);
            if (reservation == null)
                return ReservationOperationResult.FailureResult("Đặt bàn không tồn tại.", "RESERVATION_NOT_FOUND");

            if (reservation.ReservationStatus != "Pending")
                return ReservationOperationResult.FailureResult(
                    "Chỉ có thể xác nhận đặt bàn ở trạng thái chờ xác nhận.",
                    "INVALID_STATUS");

            reservation.ReservationStatus = "Confirmed";
            reservation.UpdatedAt = DateTime.UtcNow;

            await _reservationRepository.UpdateAsync(reservation);

            return ReservationOperationResult.SuccessResult("Đã xác nhận đặt bàn thành công.");
        }

        public async Task<List<Reservation>> GetCustomerReservationsAsync(int customerId)
        {
            if (customerId <= 0)
                return new List<Reservation>();

            return await _reservationRepository.GetByCustomerAsync(customerId);
        }

        public async Task<Reservation?> GetReservationDetailsAsync(int reservationId, int customerId)
        {
            if (reservationId <= 0 || customerId <= 0)
                return null;

            var reservation = await _reservationRepository.GetByIdAsync(reservationId);
            if (reservation == null)
                return null;

            if (reservation.CustomerID != customerId)
                return null;

            return reservation;
        }

        public async Task<bool> HasReservationConflictAsync(
            int tableId,
            DateTime reservationDate,
            int durationMinutes = 120)
        {
            if (tableId <= 0 || reservationDate <= BusinessClock.Now)
                return false;

            return await _reservationRepository.HasConflictAsync(
                tableId,
                reservationDate,
                reservationDate.AddMinutes(durationMinutes));
        }
    }
}
