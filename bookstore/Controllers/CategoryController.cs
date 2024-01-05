using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using bookstore.Data;
using bookstore.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;

namespace bookstore.Controllers
{
    public class CategoryController : Controller
    {

        private readonly ApplicationDbContext _db;
        // pass in our db context to construtor of controller
        // We now have access to the db via this.
        // Recall we already have all this configured in the services
        // area of "Program.cs"
        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            // var categoryList = _db.Categories
            //     .FromSql($"SELECT * FROM dbo.Categories WHERE Name = {"SciFi"}")
            //     .ToList();
            var categoryList = _db.Categories
                .FromSql($"SELECT * FROM dbo.Categories")
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
                _db.Database
                    .ExecuteSql($@"
                        INSERT INTO dbo.Categories (Name, DisplayOrder)
                        VALUES ({category.Name}, {category.DisplayOrder});
                    ");
                return RedirectToAction("Index", "Category"); // redirects to the specified ACTION of secified CONTROLLER
            }

            return View();
        }

        public IActionResult Edit(int? categoryId)
        {
            if (categoryId == null || categoryId == 0) return NotFound();

            var category = _db.Categories
                .FromSql($@"
                    SELECT * FROM dbo.Categories
                    WHERE Id = {categoryId}
                ").FirstOrDefault();

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
                _db.Database
                    .ExecuteSql($@"
                        UDATE dbo.Categories SET 
                        Name = {category.Name}, DisplayOrder = {category.DisplayOrder}
                        WHERE Id ={category.Id};
                    ");
                return RedirectToAction("Index", "Category"); // redirects to the specified ACTION of secified CONTROLLER
            }

            return View();
        }
    }
}
