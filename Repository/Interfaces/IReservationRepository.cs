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
    }
}
