using BookStore.DataAccess.Repository;

namespace BookStore.DataAccess.UnitOfWork.IUnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        ICategoryRepository Categories { get; init; }
        IProductRepository Products { get; init; }
        ICompanyRepository Companies { get; init; }

        int Complete();
    }
}