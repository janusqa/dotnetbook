using Bookstore.Models.Domain;
using BookStore.DataAccess.Repository.IRepository;

namespace BookStore.DataAccess.Repository
{
    public interface IProductRepository : IRepository<Product>
    {

    }
}