using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookstore.DataAccess.Data;
using Bookstore.Models.Domain;
using Microsoft.IdentityModel.Tokens;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.Data.SqlClient;

namespace bookstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {

        private readonly IUnitOfWork _uow;
        // pass in our db context to construtor of controller
        // We now have access to the db via this.
        // Recall we already have all this configured in the services
        // area of "Program.cs"
        public CategoryController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IActionResult Index()
        {
            // var categoryList = _db.Categories
            //     .FromSql($"SELECT * FROM dbo.Categories WHERE Name = {"SciFi"}")
            //     .ToList();
            var categoryList = _uow.Categories
                .FromSql($"SELECT * FROM dbo.Categories", [])
                .ToList();
            return View(categoryList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            // Add our own custom checks
            // Mame and ValidationOrder cannot be same
            if (!category.Name.IsNullOrEmpty() && category.Name.Equals(category.DisplayOrder.ToString(), StringComparison.CurrentCultureIgnoreCase))
            {
                // update ModelState with custome error (key, errormessage) where key 
                // is the name of the field in the model you wish to display this error for.
                ModelState.AddModelError("Name", "The Display Order cannot exactly match the Name.");
            }

            // If name is "test"
            if (!category.Name.IsNullOrEmpty() && category.Name.Equals("test", StringComparison.CurrentCultureIgnoreCase))
            {
                // update ModelState with custome error (key, errormessage) where key 
                // is the name of the field in the model you wish to display this error for.
                ModelState.AddModelError("", "Test is an invalid value.");
            }

            if (ModelState.IsValid)  // checks that inputs are valid according to annotations on the model we are working with
            {
                _uow.Categories.ExecuteSql(@"
                        INSERT INTO dbo.Categories (Name, DisplayOrder)
                        VALUES (@Name, @DisplayOrder);
                    ", [
                            new SqlParameter("Name", category.Name),
                            new SqlParameter("DisplayOrder", category.DisplayOrder)
                        ]);
                TempData["success"] = "Category created successfully"; // used for passing data in the next rendered page.
                return RedirectToAction("Index", "Category"); // redirects to the specified ACTION of secified CONTROLLER
            }

            return View();
        }

        public IActionResult Edit(int? categoryId)
        {
            if (categoryId == null || categoryId == 0) return NotFound();

            var category = _uow.Categories
                .FromSql($@"
                    SELECT * FROM dbo.Categories
                    WHERE Id = @Id
                ", [new SqlParameter("Id", categoryId)]).FirstOrDefault();

            if (category == null) return NotFound();

            return View(category);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            // Add our own custom checks
            // Mame and ValidationOrder cannot be same
            if (!category.Name.IsNullOrEmpty() && category.Name.Equals(category.DisplayOrder.ToString(), StringComparison.CurrentCultureIgnoreCase))
            {
                // update ModelState with custome error (key, errormessage) where key 
                // is the name of the field in the model you wish to display this error for.
                ModelState.AddModelError("Name", "The Display Order cannot exactly match the Name.");
            }

            // If name is "test"
            if (!category.Name.IsNullOrEmpty() && category.Name.Equals("test", StringComparison.CurrentCultureIgnoreCase))
            {
                // update ModelState with custome error (key, errormessage) where key 
                // is the name of the field in the model you wish to display this error for.
                ModelState.AddModelError("", "Test is an invalid value.");
            }

            if (ModelState.IsValid)  // checks that inputs are valid according to annotations on the model we are working with
            {
                _uow.Categories
                    .ExecuteSql($@"
                        UPDATE dbo.Categories
                        SET 
                            Name = @Name, 
                            DisplayOrder = @DisplayOrder
                        WHERE Id = @Id;
                    ", [
                        new SqlParameter("Name", category.Name),
                        new SqlParameter("DisplayOrder", category.DisplayOrder),
                        new SqlParameter("Id", category.Id)
                    ]);
                TempData["success"] = "Category updated successfully"; // used for passing data in the next rendered page.
                return RedirectToAction("Index", "Category"); // redirects to the specified ACTION of secified CONTROLLER
            }

            return View();
        }

        public IActionResult Delete(int? categoryId)
        {
            if (categoryId == null || categoryId == 0) return NotFound();

            var category = _uow.Categories
                .FromSql($@"
                    SELECT * FROM dbo.Categories
                    WHERE Id = @Id
                ", [new SqlParameter("Id", categoryId)]).FirstOrDefault();

            if (category == null) return NotFound();

            return View(category);
        }

        // We have to be careful here. We need to name this ACTION
        // as DeletePOST so it does not conflict with the above ACTION
        // which will have same NAME and PARAMS.
        // To tie this action back to the same NAME above we annote it
        // with "ActionName" and the common name for our action.
        // This effectively sets this action's name as "Delete" even though
        // it is physically named "DeletePOST"
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? categoryId)
        {
            if (categoryId == null || categoryId == 0) return NotFound();

            _uow.Categories
                .ExecuteSql($@"
                        DELETE FROM dbo.Categories
                        WHERE Id = @Id;
                    ", [new SqlParameter("Id", categoryId)]);
            TempData["success"] = "Category deleted successfully"; // used for passing data in the next rendered page.
            return RedirectToAction("Index", "Category"); // redirects to the specified ACTION of secified CONTROLLER
        }
    }
}
