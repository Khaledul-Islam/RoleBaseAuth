using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using RoleBaseAuth.Domain.Entities;
using RoleBaseAuth.Domain.Interfaces;
using RoleBaseAuth.Infrastructure.Persistence.Contexts;

namespace RoleBaseAuth.Infrastructure.Persistence.UnitOfWorks;

public class UnitOfWork(
    ApplicationDbContext context,
    ILogger<UnitOfWork> logger,
    IRepository<User> users,
    IRepository<Role> roles,
    IRepository<Permission> permissions,
    IRepository<Customer> customers,
    IRepository<RefreshToken> refreshTokens,
    IRepository<AuditLog> auditLogs)
    : IUnitOfWork
{
    private IDbContextTransaction _transaction;

    public IRepository<User> Users { get; } = users;
    public IRepository<Role> Roles { get; } = roles;
    public IRepository<Permission> Permissions { get; } = permissions;
    public IRepository<Customer> Customers { get; } = customers;
    public IRepository<RefreshToken> RefreshTokens { get; } = refreshTokens;
    public IRepository<AuditLog> AuditLogs { get; } = auditLogs;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            await _transaction?.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction?.RollbackAsync(cancellationToken);
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await action();
            await CommitTransactionAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transaction failed");
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context?.Dispose();
    }
}