using System.Text.Json;
using Bookstore.Models.Domain;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace bookstore.Areas.Admin.Controllers
{

    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {

        private readonly IUnitOfWork _uow;

        public CompanyController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Upsert(int? entityId)
        {
            Company? company = entityId is null || entityId == 0
                ? new Company()
                : _uow.Companies
                .FromSql($@"
                    SELECT * FROM dbo.Companies
                    WHERE Id = @Id
                ", [new SqlParameter("Id", entityId)]).FirstOrDefault();

            if (company is null) return NotFound();

            return View(company);
        }

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            // Add our own custom checks if any

            if (ModelState.IsValid)  // checks that inputs are valid according to annotations on the model we are working with
            {
                _uow.Companies.ExecuteSql(@"
                        MERGE INTO Companies AS target
                        USING 
                            (
                                VALUES (@Id, @Name, @StreetAddress, @City, @State, @PostalCode, @PhoneNumber)
                            ) AS source (Id, Name, StreetAddress, City, State, PostalCode, PhoneNumber)
                        ON target.Id = source.Id
                        WHEN MATCHED THEN
                            UPDATE SET 
                                target.Name = source.Name, 
                                target.StreetAddress = source.StreetAddress, 
                                target.City = source.City, 
                                target.State = source.State, 
                                target.PostalCode = source.PostalCode, 
                                target.PhoneNumber = source.PhoneNumber
                        WHEN NOT MATCHED THEN
                            INSERT (Name, StreetAddress, City, State, PostalCode, PhoneNumber) 
                                    VALUES (source.Name, source.StreetAddress, source.City, source.State, source.PostalCode, source.PhoneNumber);
                    ", [
                            new SqlParameter("Id", company.Id),
                            new SqlParameter("Name", company.Name),
                            new SqlParameter("StreetAddress", company.StreetAddress),
                            new SqlParameter("City", company.City),
                            new SqlParameter("State", company.State),
                            new SqlParameter("PostalCode", company.PostalCode),
                            new SqlParameter("PhoneNumber", company.PhoneNumber),
                        ]);
                TempData["success"] = $"Company {(company.Id == 0 ? "created" : "updated")} successfully"; // used for passing data in the next rendered page.
                return RedirectToAction("Index", "Company"); // redirects to the specified ACTION of secified CONTROLLER
            }

            return View(company);
        }


        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            var companyList = _uow.Companies
            .FromSql(@$"
                SELECT 
                    *
                FROM dbo.Companies;
            ", [])?
            .ToList();

            return Json(new { data = companyList ?? [] });
        }

        [HttpDelete]
        public IActionResult Delete([FromBody] JsonElement requestBody)
        {
            int? entityId = requestBody.GetProperty("entityId").GetInt32();
            if (entityId is null || entityId == 0) return Json(new { success = false, message = "Not found" });

            _uow.Companies.ExecuteSql($@"
                    DELETE FROM dbo.Companies  WHERE Id = @Id;
                ", [new SqlParameter("Id", entityId)]);

            return Json(new { success = true, message = "Delete successful" });
        }

        #endregion
    }
}