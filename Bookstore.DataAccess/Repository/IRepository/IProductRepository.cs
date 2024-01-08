using Bookstore.Models.Domain;
using BookStore.DataAccess.Repository.IRepository;
using Microsoft.Data.SqlClient;

namespace BookStore.DataAccess.Repository
{
    public interface IProductRepository : IRepository<Product>
    {
    }
}