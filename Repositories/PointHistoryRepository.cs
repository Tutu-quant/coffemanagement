using CafeManagement.Data;
using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories
{
    /// <summary>
    /// Repository implementation for PointHistory entity operations.
    /// </summary>
    public class PointHistoryRepository : GenericRepository<PointHistory>, IPointHistoryRepository
    {
        public PointHistoryRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
