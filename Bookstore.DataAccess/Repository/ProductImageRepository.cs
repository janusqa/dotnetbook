using Bookstore.DataAccess.Data;
using Bookstore.Models.Domain;

namespace BookStore.DataAccess.Repository
{
    public class ProductImageRepository : Repository<ProductImage>, IProductImageRepository
    {
        public ProductImageRepository(ApplicationDbContext db) : base(db)
        {

        }

    }
}