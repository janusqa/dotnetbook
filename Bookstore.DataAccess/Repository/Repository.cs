using System.Linq.Expressions;
using Bookstore.DataAccess.Data;
using BookStore.DataAccess.Repository.IRepository;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookStore.DataAccess.Repository
{

    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;

        public Repository(ApplicationDbContext db)
        {
            _db = db;
        }

        public T? Get(int Id)
        {
            return _db.Set<T>().Find(Id);
        }

        public IEnumerable<T> GetAll()
        {
            return _db.Set<T>().ToList();
        }

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate)
        {
            return _db.Set<T>().Where(predicate);
        }

        public void Add(T entity)
        {
            _db.Set<T>().Add(entity);
        }

        public void AddRange(IEnumerable<T> entities)
        {
            _db.Set<T>().AddRange(entities);
        }

        public void Remove(T entity)
        {
            _db.Set<T>().Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            _db.Set<T>().RemoveRange(entities);
        }

        public IEnumerable<T> FromSql(string sql, List<SqlParameter> sqlParameters)
        {
            return _db.Set<T>().FromSqlRaw(sql, sqlParameters.ToArray()).ToList();
        }

        public void ExecuteSql(string sql, List<SqlParameter> sqlParameters)
        {
            _db.Database.ExecuteSqlRaw(sql, sqlParameters.ToArray());
        }
    }
}