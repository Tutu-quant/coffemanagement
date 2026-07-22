using CafeManagement.Models.Entities;
using CafeManagement.Repositories.Interfaces;

namespace CafeManagement.Repositories.Interfaces
{
    /// <summary>
    /// Repository interface for Customer entity operations.
    /// </summary>
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
    }
}
