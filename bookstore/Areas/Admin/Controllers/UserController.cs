using System.Text;
using System.Text.Json;
using Bookstore.DataAccess.Data;
using Bookstore.Models.Identity;
using Bookstore.Models.ViewModels;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;

namespace bookstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(IUnitOfWork uow, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _uow = uow;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagement(string entityId)
        {
            var user = _uow.ApplicationUsers
            .SqlQuery<ApplicationUserWithRole>(@$"
                SELECT 
                    u.*,
                    COALESCE(r.Id,'') AS RoleId,
                    r.Name AS RoleName
                FROM 
                    dbo.AspNetUserRoles ur 
                INNER JOIN
                    dbo.AspNetRoles r ON (r.Id = ur.RoleId)
                RIGHT JOIN
                    dbo.AspNetUsers u ON (u.Id = ur.UserId) 
                WHERE u.Id = @Id
            ", [new SqlParameter("Id", entityId)])?
            .ToList().FirstOrDefault();

            if (user is null) return NotFound();

            var Roles =
                _roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Id
                });

            var Companies = _uow.Companies.FromSql($@"
                    SELECT * FROM dbo.Companies;
                ", []).Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });

            var UserRoleView = new UserRoleViewModel
            {
                User = user,
                Roles = Roles,
                Companies = Companies,
            };

            return View(UserRoleView);
        }

        [HttpPost]
        public async Task<IActionResult> RoleManagement(UserRoleViewModel userRoleView)
        {
            // construct dropdown list for view
            var Roles = _roleManager.Roles
                .Select(r => new SelectListItem
                {
                    Text = r.Name,
                    Value = r.Id
                });

            var Companies = _uow.Companies.FromSql($@"
                SELECT * FROM dbo.Companies;
            ", []).Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            });

            userRoleView.Roles = Roles;
            userRoleView.Companies = Companies;


            // get new role name, and comanyId if applicable
            var roleName = Roles.Where(r => r.Value == userRoleView.User.RoleId).Select(r => r.Text).FirstOrDefault();
            var companyId = roleName == SD.Role_Company ? userRoleView.User.CompanyId : null;

            using var transaction = _uow.Context().BeginTransaction();

            _uow.ApplicationUsers.ExecuteSql($@"
                UPDATE dbo.AspNetUsers
                    SET CompanyID = @CompanyId
                WHERE Id = @Id
            ", [
                new SqlParameter("Id", userRoleView.User.Id),
                new SqlParameter("@CompanyId", companyId ?? (object)DBNull.Value)
            ]);

            var user = await _userManager.FindByIdAsync(userRoleView.User.Id);
            var oldRoleId = user is not null ? (await _userManager.GetRolesAsync(user)).FirstOrDefault() : null;

            if (
                user is not null && oldRoleId is not null && roleName is not null)
            {
                await _userManager.RemoveFromRoleAsync(user, oldRoleId);
                await _userManager.AddToRoleAsync(user, roleName);
            }

            transaction.Commit();

            return RedirectToAction(nameof(Index), "User");
            //return View(userRoleView);
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _uow.ApplicationUsers
            .SqlQuery<ApplicationUserWithRole>(@$"
                SELECT 
                    u.*,
                    COALESCE(r.Id,'') AS RoleId,
                    r.Name AS RoleName
                FROM 
                    dbo.AspNetUserRoles ur 
                INNER JOIN
                    dbo.AspNetRoles r ON (r.Id = ur.RoleId)
                RIGHT JOIN
                    dbo.AspNetUsers u ON (u.Id = ur.UserId) 
            ", [])?
            .ToList();

            if (users is not null && users.Count > 0)
            {
                var companyIds = users.Where(u => u.CompanyId is not null).Select(u => u.CompanyId).ToList();
                var inParams = new StringBuilder();
                var sqlParams = new List<SqlParameter>();
                foreach (var (Id, idx) in companyIds.Select((Id, idx) => (Id, idx)))
                {
                    inParams.Append($"@p{idx},");
                    sqlParams.Add(new SqlParameter($"p{idx}", Id));
                }
                var companies = _uow.Companies.FromSql($@"
                    SELECT * 
                    FROM dbo.Companies
                    WHERE Id IN ({inParams.ToString()[..^1]});
                ", sqlParams).ToDictionary(c => c.Id, c => c);

                foreach (var user in users)
                {
                    if (user.CompanyId is not null) user.Company = companies[user.CompanyId.Value];
                };
            }

            return Json(new { data = users ?? [] });
        }

        [HttpPost]
        public IActionResult LockUser([FromBody] JsonElement requestBody)
        {
            JsonElement entityIdElement = requestBody.GetProperty("entityId");
            string? entityId = entityIdElement.ValueKind == JsonValueKind.Null ? null : entityIdElement.GetString();

            JsonElement lockoutEndElement = requestBody.GetProperty("lockoutEnd");
            string? lockoutEnd = lockoutEndElement.ValueKind == JsonValueKind.Null ? null : lockoutEndElement.GetString();

            if (entityId is null) return Json(new { success = false, message = "Not found" });

            var sqlLockoutParam = new SqlParameter { ParameterName = "LockoutEnd" };

            if (DateTime.TryParse(lockoutEnd, out DateTime lockoutEndDate) && lockoutEndDate > DateTime.Now)
            {
                // unlock user
                sqlLockoutParam.Value = DateTime.Now;
            }
            else
            {
                // lock user
                sqlLockoutParam.Value = DateTime.Now.AddYears(1000);
            }

            _uow.ApplicationUsers.ExecuteSql($@"
                UPDATE dbo.AspNetUsers 
                SET
                    LockoutEnd = @LockoutEnd
                WHERE Id = @Id
            ", [
                new SqlParameter("Id", entityId),
                sqlLockoutParam
            ]);

            return Json(new { success = true, message = "Updated successfully" });
        }

        #endregion
    }
}