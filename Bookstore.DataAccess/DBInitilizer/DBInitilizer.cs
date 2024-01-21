using Bookstore.DataAccess.Data;
using Bookstore.Models.Identity;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookStore.DataAccess.DBInitilizer
{
    public class DBInitilizer : IDBInitilizer
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;
        private readonly IUnitOfWork _uow;

        public DBInitilizer(
            UserManager<ApplicationUser> um,
            RoleManager<IdentityRole> rm,
            ApplicationDbContext db,
            IUnitOfWork uow
        )
        {
            _userManager = um;
            _roleManager = rm;
            _db = db;
            _uow = uow;
        }

        public async Task Initilize()
        {
            // 1. Run any unapplied migrations
            try
            {
                if (_db.Database.GetPendingMigrations().Any()) _db.Database.Migrate();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Migration Error: {ex.Message}");
            }

            // 2. Create Roles if the do not already exist
            var roles = new List<string> {
                SD.Role_Customer,
                SD.Role_Company,
                SD.Role_Admin,
                SD.Role_Employee,
            };

            var RolesToCreate = roles
                .Where(r => !_roleManager.RoleExistsAsync(r).GetAwaiter().GetResult())
                .ToList();

            foreach (var task in RolesToCreate)
            {
                await _roleManager.CreateAsync(new IdentityRole(task));
            }

            // 3. If creating admin role for first time, also create first admin account
            if (RolesToCreate.Contains(SD.Role_Admin))
            {

                var adminEmail = "your@email.com";
                await _userManager.CreateAsync(new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "Janus QA",
                    PhoneNumber = "1-234-567-8901",
                    StreetAddress = "1st Ave. Blindspot",
                    City = "Bridgetown",
                    PostalCode = "BGI",
                    State = "St. Michael"
                }, "your_password");

                var user = _uow.ApplicationUsers.FromSql($@"
                    SELECT * from dbo.AspNetUsers WHERE Email = @Email
                ", [new SqlParameter("Email", adminEmail)]).FirstOrDefault();
                if (user is not null) await _userManager.AddToRoleAsync(user, SD.Role_Admin);
            }

            return;
        }
    }
}