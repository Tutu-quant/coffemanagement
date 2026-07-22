namespace CafeManagement.Repositories.Interfaces
{
    /// <summary>
    /// Generic repository interface for CRUD operations.
    /// Provides common data access methods for all entities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets all entities asynchronously.
        /// </summary>
        Task<IEnumerable<TEntity>> GetAllAsync();

        /// <summary>
        /// Gets an entity by its primary key asynchronously.
        /// </summary>
        /// <param name="id">The primary key value</param>
        Task<TEntity?> GetByIdAsync(object id);

        /// <summary>
        /// Adds a new entity asynchronously.
        /// </summary>
        /// <param name="entity">The entity to add</param>
        Task AddAsync(TEntity entity);

        /// <summary>
        /// Adds multiple entities asynchronously.
        /// </summary>
        /// <param name="entities">The entities to add</param>
        Task AddRangeAsync(IEnumerable<TEntity> entities);

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        /// <param name="entity">The entity to update</param>
        void Update(TEntity entity);

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        /// <param name="entity">The entity to delete</param>
        void Delete(TEntity entity);

        /// <summary>
        /// Deletes multiple entities.
        /// </summary>
        /// <param name="entities">The entities to delete</param>
        void DeleteRange(IEnumerable<TEntity> entities);

        /// <summary>
        /// Finds entities matching the specified predicate asynchronously.
        /// </summary>
        /// <param name="predicate">The filter predicate</param>
        Task<IEnumerable<TEntity>> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Checks if any entity exists matching the specified predicate asynchronously.
        /// </summary>
        /// <param name="predicate">The filter predicate</param>
        Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate);

        /// <summary>
        /// Counts entities matching the specified predicate asynchronously.
        /// </summary>
        /// <param name="predicate">Optional filter predicate</param>
        Task<int> CountAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>>? predicate = null);

        /// <summary>
        /// Saves changes to the database asynchronously.
        /// </summary>
        Task SaveChangesAsync();
    }
}
