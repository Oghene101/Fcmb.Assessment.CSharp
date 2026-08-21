using System.Linq.Expressions;

namespace Fcmb.Assessment.CSharp.Common.Application.Data;

public interface IRepository<TEntity> where TEntity : class
{
    Task AddAsync(
        TEntity entity, 
        CancellationToken cancellationToken = default);
    
    void Update(
        TEntity entity, 
        params Expression<Func<TEntity, object>>[] updatedProperties);
    
    void Delete(TEntity entity);
}
