namespace Quản_lý_quán_cafe.Services.Interfaces
{
    public interface IReservationService
    {
        Task<(bool Success, string Message)> CreateReservationAsync(object reservationData);
        Task<(bool Success, string Message)> UpdateReservationAsync(int id, object data);
        Task<(bool Success, string Message)> CancelReservationAsync(int id);
    }
}
