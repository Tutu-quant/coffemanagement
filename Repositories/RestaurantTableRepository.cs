using CafeManagement.Data;
using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories
{
    /// <summary>
    /// Repository implementation for RestaurantTable entity operations.
    /// </summary>
    public class RestaurantTableRepository : GenericRepository<RestaurantTable>, IRestaurantTableRepository
    {
        public RestaurantTableRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
