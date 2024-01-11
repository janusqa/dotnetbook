using Bookstore.DataAccess.Data;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;

namespace BookStore.DataAccess.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;

        public ICategoryRepository Categories { get; init; }
        public IProductRepository Products { get; init; }
        public ICompanyRepository Companies { get; init; }
        public IShoppingCartRepository ShoppingCarts { get; init; }
        public IApplicationUserRepository ApplicationUsers { get; init; }

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;

            Categories = new CategoryRepository(_db);
            Products = new ProductRepository(_db);
            Companies = new CompanyRepository(_db);
            ShoppingCarts = new ShoppingCartRepository(_db);
            ApplicationUsers = new ApplicationUserRepository(_db);
        }

        public int Complete()
        {
            return _db.SaveChanges();
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}