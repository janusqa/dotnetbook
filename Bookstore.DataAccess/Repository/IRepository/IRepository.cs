using System.Linq.Expressions;
using Microsoft.Data.SqlClient;

namespace BookStore.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        T? Get(int Id);
        IEnumerable<T> GetAll();
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);

        void Add(T entity);
        void AddRange(IEnumerable<T> entities);

        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

        IEnumerable<T> FromSql(string sql, List<SqlParameter> sqlParameters);
        void ExecuteSql(string sql, List<SqlParameter> sqlParameters);

        IEnumerable<U>? SqlQuery<U>(string sql, List<SqlParameter> sqlParameters);
    }
}