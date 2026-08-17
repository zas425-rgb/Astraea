namespace Astraea.Application.Abstractions;

public interface IRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
    void Remove(T entity);
}

public interface IUnitOfWork
{
    IRepository<T> Repository<T>()
        where T : class;

    Task<int> SaveChangesAsync(CancellationToken ct);
}
