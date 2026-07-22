using Microsoft.EntityFrameworkCore;
using CafeManagement.Data;
using System.Linq.Expressions;

namespace CafeManagement.Repositories
{
    /// <summary>
    /// Generic repository implementation for CRUD operations.
    /// Provides common data access methods for all entities.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<TEntity> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = context.Set<TEntity>();
        }

        /// <summary>
        /// Gets all entities asynchronously.
        /// </summary>
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        /// <summary>
        /// Gets an entity by its primary key asynchronously.
        /// </summary>
        public async Task<TEntity?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>
        /// Adds a new entity asynchronously.
        /// </summary>
        public async Task AddAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await _dbSet.AddAsync(entity);
        }

        /// <summary>
        /// Adds multiple entities asynchronously.
        /// </summary>
        public async Task AddRangeAsync(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            await _dbSet.AddRangeAsync(entities);
        }

        /// <summary>
        /// Updates an existing entity.
        /// </summary>
        public void Update(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Update(entity);
        }

        /// <summary>
        /// Deletes an entity.
        /// </summary>
        public void Delete(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _dbSet.Remove(entity);
        }

        /// <summary>
        /// Deletes multiple entities.
        /// </summary>
        public void DeleteRange(IEnumerable<TEntity> entities)
        {
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));

            _dbSet.RemoveRange(entities);
        }

        /// <summary>
        /// Finds entities matching the specified predicate asynchronously.
        /// </summary>
        public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return await _dbSet.Where(predicate).ToListAsync();
        }

        /// <summary>
        /// Checks if any entity exists matching the specified predicate asynchronously.
        /// </summary>
        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return await _dbSet.AnyAsync(predicate);
        }

        /// <summary>
        /// Counts entities matching the specified predicate asynchronously.
        /// </summary>
        public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();

            return await _dbSet.CountAsync(predicate);
        }

        /// <summary>
        /// Saves changes to the database asynchronously.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
