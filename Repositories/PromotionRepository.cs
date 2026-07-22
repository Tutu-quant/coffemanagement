using CafeManagement.Data;
using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories
{
    /// <summary>
    /// Repository implementation for Promotion entity operations.
    /// </summary>
    public class PromotionRepository : GenericRepository<Promotion>, IPromotionRepository
    {
        public PromotionRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
