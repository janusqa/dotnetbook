using Bookstore.DataAccess.Data;
using Bookstore.Models.Domain;

namespace BookStore.DataAccess.Repository
{
    public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
    {
        public ShoppingCartRepository(ApplicationDbContext db) : base(db)
        {

        }
    }
}