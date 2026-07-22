using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for Category entity operations.
    /// </summary>
    public interface ICategoryRepository : IGenericRepository<Category>
    {
    }
}
