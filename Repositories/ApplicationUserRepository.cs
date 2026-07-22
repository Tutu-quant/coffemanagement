using CafeManagement.Data;
using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories
{
    /// <summary>
    /// Repository implementation for ApplicationUser entity operations.
    /// </summary>
    public class ApplicationUserRepository : GenericRepository<ApplicationUser>, IApplicationUserRepository
    {
        public ApplicationUserRepository(ApplicationDbContext context) : base(context)
        {
        }
    }
}
