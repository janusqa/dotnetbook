using System.Security.Claims;
using System.Text;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace bookstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    // this protects this controller from unauthorized roles. 
    // If we need finer grained control we can apply it instead directly to actions in the controller
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _uow;

        public OrderController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var orderHeaders = _uow.OrderHeaders.FromSql(@$"
                SELECT *FROM dbo.OrderHeaders
            ", []).ToList();

            var applicationUserIds = orderHeaders.Select(oh => oh.ApplicationUserId);
            var inParams = new StringBuilder();
            var sqlParams = new List<SqlParameter>();
            foreach (var (Id, idx) in applicationUserIds.Select((Id, idx) => (Id, idx)))
            {
                inParams.Append($"@p{idx},");
                sqlParams.Add(new SqlParameter($"p{idx}", Id));
            }
            var ApplicationUsers = _uow.ApplicationUsers.FromSql($@"
                SELECT * FROM dbo.AspNetUsers WHERE Id in ({inParams.ToString()[..^1]})
            ", sqlParams).ToDictionary(au => au.Id, au => au);

            foreach (var orderHeader in orderHeaders)
            {
                orderHeader.ApplicationUser = ApplicationUsers[orderHeader.ApplicationUserId];
            }

            return Json(new { data = orderHeaders ?? [] });
        }

        #endregion
    }
}