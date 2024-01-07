using Bookstore.Models.Domain;
using BookStore.DataAccess.Repository.IRepository;

namespace BookStore.DataAccess.Repository
{
    public interface ICategoryRepository : IRepository<Category>
    {

    }
}