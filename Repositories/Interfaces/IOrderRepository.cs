using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for Order entity operations.
    /// </summary>
    public interface IOrderRepository : IGenericRepository<Order>
    {
    }
}
