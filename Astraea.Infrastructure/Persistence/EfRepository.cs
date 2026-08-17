using Astraea.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Astraea.Infrastructure.Persistence;

public sealed class EfRepository<T>(AstraeaDbContext context) : IRepository<T>
    where T : class
{
    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return context.Set<T>().FindAsync([id], ct).AsTask();
    }

    public async Task<IReadOnlyList<T>> ListAsync(CancellationToken ct)
    {
        return await context.Set<T>().AsNoTracking().ToListAsync(ct);
    }

    public Task AddAsync(T entity, CancellationToken ct)
    {
        return context.Set<T>().AddAsync(entity, ct).AsTask();
    }

    public void Remove(T entity)
    {
        context.Set<T>().Remove(entity);
    }
}

public sealed class EfUnitOfWork(AstraeaDbContext context) : IUnitOfWork
{
    public IRepository<T> Repository<T>()
        where T : class
    {
        return new EfRepository<T>(context);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct)
    {
        return context.SaveChangesAsync(ct);
    }
}
