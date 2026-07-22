using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for Reservation entity operations.
    /// </summary>
    public interface IReservationRepository : IGenericRepository<Reservation>
    {
    }
}
