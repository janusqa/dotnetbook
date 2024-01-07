using Bookstore.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Bookstore.DataAccess.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        // Create a table called Categories in the database.
        // Dbset represents the table we want to create when migrations run.
        public DbSet<Category> Categories { get; set; }

        // use this to seed the Category table with some data if required. 
        // We created this ourselves. It was not there initially.
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Action", DisplayOrder = 1 },
                new Category { Id = 2, Name = "SciFi", DisplayOrder = 2 },
                new Category { Id = 3, Name = "History", DisplayOrder = 3 }
            );
        }
    }
}