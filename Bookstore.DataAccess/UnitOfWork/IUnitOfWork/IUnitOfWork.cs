using BookStore.DataAccess.Repository;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace BookStore.DataAccess.UnitOfWork.IUnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Categories { get; init; }
        IProductRepository Products { get; init; }
        ICompanyRepository Companies { get; init; }
        IShoppingCartRepository ShoppingCarts { get; init; }
        IApplicationUserRepository ApplicationUsers { get; init; }
        IOrderHeaderRepository OrderHeaders { get; init; }
        IOrderDetailRepository OrderDetails { get; init; }

        int Complete();

        DatabaseFacade Context();
    }
}