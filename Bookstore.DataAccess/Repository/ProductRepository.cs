using Bookstore.DataAccess.Data;
using Bookstore.Models.Domain;
using Microsoft.Data.SqlClient;

namespace BookStore.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext db) : base(db)
        {

        }
    }
}