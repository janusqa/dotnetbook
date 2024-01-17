using System.Text;
using System.Text.Json;
using Bookstore.DataAccess.Data;
using Bookstore.Models.Identity;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace bookstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly ApplicationDbContext _db;

        public UserController(IUnitOfWork uow, ApplicationDbContext db)
        {
            _uow = uow;
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _uow.ApplicationUsers
            .SqlQuery<ApplicationUserWithRole>(@$"
                SELECT 
                    u.*,
                    r.Id AS RoleId,
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
        public IActionResult Lock([FromBody] JsonElement requestBody)
        {
            int? entityId = requestBody.GetProperty("entityId").GetInt32();
            string? lockoutEnd = requestBody.GetProperty("lockoutEnd").GetString();
            if (entityId is null || entityId == 0) return Json(new { success = false, message = "Not found" });

            var sqlLockoutParam = new SqlParameter { ParameterName = "lockoutEnd" };

            if (lockoutEnd is not null && DateTime.Parse(lockoutEnd) > DateTime.Now)
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