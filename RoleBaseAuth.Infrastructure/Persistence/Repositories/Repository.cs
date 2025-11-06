using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RoleBaseAuth.Domain.Common;
using RoleBaseAuth.Domain.Interfaces;
using RoleBaseAuth.Infrastructure.Persistence.Contexts;
using System.Linq.Expressions;

namespace RoleBaseAuth.Infrastructure.Persistence.Repositories;

public class Repository<T>(ApplicationDbContext context, ILogger<Repository<T>> logger)
    : IRepository<T>
    where T : BaseEntity
{
    protected readonly ApplicationDbContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();
    private readonly ILogger<Repository<T>> _logger = logger;

    public virtual async Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await Task.CompletedTask;
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
        }
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>> predicate = null,
        CancellationToken cancellationToken = default)
    {
        if (predicate == null)
            return await _dbSet.CountAsync(cancellationToken);

        return await _dbSet.CountAsync(predicate, cancellationToken);
    }
}