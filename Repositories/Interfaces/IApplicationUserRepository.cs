using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for ApplicationUser entity operations.
    /// </summary>
    public interface IApplicationUserRepository : IGenericRepository<ApplicationUser>
    {
    }
}
