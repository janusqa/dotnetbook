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
    public class ProductController : Controller
    {

        private readonly IUnitOfWork _uow;
        // pass in our db context to construtor of controller
        // We now have access to the db via this.
        // Recall we already have all this configured in the services
        // area of "Program.cs"
        public ProductController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IActionResult Index()
        {
            var productList = _uow.Products
                .FromSql($"SELECT * FROM dbo.Products", [])
                .ToList();
            return View(productList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Product product)
        {
            // Add our own custom checks if any

            if (ModelState.IsValid)  // checks that inputs are valid according to annotations on the model we are working with
            {
                _uow.Products.ExecuteSql(@"
                        INSERT INTO dbo.Products (Title, Description, ISBN, Author, ListPrice, Price, Price50, Price100)
                        VALUES (@Title, @Description, @ISBN, @Author, @ListPrice, @Price, @Price50, @Price100);
                    ", [
                            new SqlParameter("Title", product.Title),
                            new SqlParameter("Description", product.Description),
                            new SqlParameter("ISBN", product.ISBN),
                            new SqlParameter("Author", product.Author),
                            new SqlParameter("ListPrice", product.ListPrice),
                            new SqlParameter("Price", product.Price),
                            new SqlParameter("Price50", product.Price50),
                            new SqlParameter("Price100", product.Price100)
                        ]);
                TempData["success"] = "Product created successfully"; // used for passing data in the next rendered page.
                return RedirectToAction("Index", "Product"); // redirects to the specified ACTION of secified CONTROLLER
            }

            return View();
        }

        public IActionResult Edit(int? entityId)
        {
            if (entityId == null || entityId == 0) return NotFound();

            var product = _uow.Products
                .FromSql($@"
                    SELECT * FROM dbo.Products
                    WHERE Id = @Id
                ", [new SqlParameter("Id", entityId)]).FirstOrDefault();

            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        public IActionResult Edit(Product product)
        {
            // Add our own custom checks

            if (ModelState.IsValid)  // checks that inputs are valid according to annotations on the model we are working with
            {
                _uow.Products
                    .ExecuteSql($@"
                        UPDATE dbo.Products 
                        SET 
                            Title = @Title, 
                            Description = @Description, 
                            ISBN = @ISBN, 
                            Author = @Author, 
                            ListPrice = @ListPrice, 
                            Price = @Price, 
                            Price50 = @Price50, 
                            Price100  = @Price100
                        WHERE Id = @Id;
                    ", [
                            new SqlParameter("Title", product.Title),
                            new SqlParameter("Description", product.Description),
                            new SqlParameter("ISBN", product.ISBN),
                            new SqlParameter("Author", product.Author),
                            new SqlParameter("ListPrice", product.ListPrice),
                            new SqlParameter("Price", product.Price),
                            new SqlParameter("Price50", product.Price50),
                            new SqlParameter("Price100", product.Price100),
                            new SqlParameter("Id", product.Id)
                    ]);
                TempData["success"] = "Product updated successfully"; // used for passing data in the next rendered page.
                return RedirectToAction("Index", "Product"); // redirects to the specified ACTION of secified CONTROLLER
            }

            return View();
        }

        public IActionResult Delete(int? entityId)
        {
            if (entityId == null || entityId == 0) return NotFound();

            var product = _uow.Products
                .FromSql($@"
                    SELECT * FROM dbo.Products
                    WHERE Id = @Id
                ", [new SqlParameter("Id", entityId)]).FirstOrDefault();

            if (product == null) return NotFound();

            return View(product);
        }

        // We have to be careful here. We need to name this ACTION
        // as DeletePOST so it does not conflict with the above ACTION
        // which will have same NAME and PARAMS.
        // To tie this action back to the same NAME above we annote it
        // with "ActionName" and the common name for our action.
        // This effectively sets this action's name as "Delete" even though
        // it is physically named "DeletePOST"
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? entityId)
        {
            if (entityId == null || entityId == 0) return NotFound();

            _uow.Products
                .ExecuteSql($@"
                        DELETE FROM dbo.Products 
                        WHERE Id = @Id;
                    ", [new SqlParameter("Id", entityId)]);
            TempData["success"] = "Product deleted successfully"; // used for passing data in the next rendered page.
            return RedirectToAction("Index", "Product"); // redirects to the specified ACTION of secified CONTROLLER
        }
    }
}
