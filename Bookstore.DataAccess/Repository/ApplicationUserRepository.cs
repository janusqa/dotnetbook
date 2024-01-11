using Bookstore.DataAccess.Data;
using Bookstore.Models.Domain;
using Bookstore.Models.Identity;

namespace BookStore.DataAccess.Repository
{
    public class ApplicationUserRepository : Repository<ApplicationUser>, IApplicationUserRepository
    {
        public ApplicationUserRepository(ApplicationDbContext db) : base(db)
        {

        }

    }
}