namespace StartupAgent.Data.Repositories;

/// <summary>
/// Generic repository interface for CRUD operations.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get entity by primary key.
    /// </summary>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Get all entities.
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Add a new entity.
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Update an existing entity.
    /// </summary>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Delete an entity by primary key.
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// Save changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync();
}
