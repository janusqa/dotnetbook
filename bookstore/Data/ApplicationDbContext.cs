using bookstore.Models;
using Microsoft.EntityFrameworkCore;

namespace bookstore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        // Create a table called Categories in the database
        public DbSet<Category> Categories { get; set; }
    }
}