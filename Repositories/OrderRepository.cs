using CafeManagement.Data;
using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories
{
    /// <summary>
    /// Repository implementation for Order entity operations.
    /// </summary>
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
