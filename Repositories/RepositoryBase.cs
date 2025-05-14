using junimo_v3.Data;
using junimo_v3.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace junimo_v3.Repositories
{
    public class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected readonly RepositoryContext RepositoryContext;
        public RepositoryBase(RepositoryContext repositoryContext)
        {
            RepositoryContext = repositoryContext;
        }
        public void Create(T entity)
        {
            RepositoryContext.Set<T>().Add(entity);
        }
        public void Delete(T entity)
        {
            RepositoryContext.Set<T>().Remove(entity);
        }
        public void Update(T entity)
        {
            RepositoryContext.Set<T>().Update(entity);
        }
        public IQueryable<T> FindAll()
        {
            return RepositoryContext.Set<T>();
        }
        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
        {
            return RepositoryContext.Set<T>().Where(expression);
        }
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await RepositoryContext.Set<T>().ToListAsync();
        }
        public async Task<T>? GetByIdAsync(int id)
        {
            return await RepositoryContext.Set<T>().FindAsync(id);
        }
        public async Task<T>? GetByIdAsync(string id)
        {
            return await RepositoryContext.Set<T>().FindAsync(id);
        }
    }
}
