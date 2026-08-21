using System.Linq.Expressions;
using Fcmb.Assessment.CSharp.Common.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure;

public sealed class Repository<TEntity, TDbContext>(
    TDbContext context) : IRepository<TEntity>
    where TEntity : class
    where TDbContext : DbContext
{
    private readonly DbSet<TEntity> _dbSet = context.Set<TEntity>();

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity, params Expression<Func<TEntity, object>>[] updatedProperties)
    {
        foreach (Expression<Func<TEntity, object>> property in updatedProperties)
        {
            context.Entry(entity).Property(property).IsModified = true;
        }
    }

    public void Delete(TEntity entity)
    {
        _dbSet.Remove(entity);
    }
}
