using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for Product entity operations.
    /// </summary>
    public interface IProductRepository : IGenericRepository<Product>
    {
    }
}
