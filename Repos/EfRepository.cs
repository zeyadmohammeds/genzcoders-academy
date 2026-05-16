using GenZCoders.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GenZCoders.Repos;

public class EfRepository<T>(AcademyDbContext db) : IRepository<T> where T : class
{
    public IQueryable<T> Query() => db.Set<T>().AsQueryable();

    public async Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.Set<T>().FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? filter = null, CancellationToken cancellationToken = default)
    {
        var query = filter is null ? db.Set<T>() : db.Set<T>().Where(filter);
        return await query.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await db.Set<T>().AddAsync(entity, cancellationToken);

    public void Update(T entity) => db.Set<T>().Update(entity);

    public void Remove(T entity) => db.Set<T>().Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}
