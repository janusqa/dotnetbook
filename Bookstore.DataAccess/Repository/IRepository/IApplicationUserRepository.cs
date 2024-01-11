using Bookstore.Models.Domain;
using Bookstore.Models.Identity;
using BookStore.DataAccess.Repository.IRepository;

namespace BookStore.DataAccess.Repository
{
    public interface IApplicationUserRepository : IRepository<ApplicationUser>
    {

    }
}