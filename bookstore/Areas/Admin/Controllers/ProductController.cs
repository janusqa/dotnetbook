using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookstore.DataAccess.Data;
using Bookstore.Models.Domain;
using Microsoft.IdentityModel.Tokens;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bookstore.Models.ViewModels;

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
                .FromSql(@$"
                    SELECT * 
                    FROM dbo.Products;
                ", [])
                .ToList();

            return View(productList);
        }

        // for Create/Edit for product we will handle it differently to Category.
        // We will use one View and One controller named Upsert that will handle
        // The fuctionaility of both creating and updating.
        public IActionResult Upsert(int? entityId)
        {
            Product? product = entityId is null || entityId == 0
                ? new Product()
                : _uow.Products
                .FromSql($@"
                    SELECT * FROM dbo.Products
                    WHERE Id = @Id
                ", [new SqlParameter("Id", entityId)]).FirstOrDefault();

            if (product is null) return NotFound();

            IEnumerable<SelectListItem> CategoryList =
                _uow.Categories
                .FromSql(@$"
                    SELECT * 
                    FROM dbo.Categories;
                ", [])
                .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

            // ViewBag. use viewbag to pass extra information to 
            // view as since we can only past one object via View()
            // ViewBag transfers data from the controller to view, 
            // and not vice versa. Ideal for situations in which 
            // the temporary data is not in a model. Lifetime is
            // only during request and will be null on redirection

            // ViewData. Alternatively to ViewBag we can use ViewData
            // It is derived from ViewDataDictionary, and it must be 
            // typecast before use. Lifetime isonly during request 
            // and will be null on redirection

            // TempData.  Another alternative is to use TempData
            // to past one time messages to the view. We have 
            // use it to pass error/success messages before
            // but it can also be used for scenarios like this

            // AViewModel. As good as the above are they do not scale properly.
            // So a better option perhaps is to use ViewModel.

            // Note CategoryList is the key to access the data in the ViewBag.
            // It is a key we made up. It can be any string.
            // Use ViewModels. This is for demo only to show how ViewBag works
            ViewBag.CategoryList = CategoryList;

            // Note no need to use both VieBag and ViewData here
            // Using ViewData is for Demo purposes righ now.
            ViewData["CategoryList"] = CategoryList;

            var ProductView = new ProductViewModel
            {
                CategoryList = CategoryList,
                Product = product
            };

            return View(ProductView);
        }

        [HttpPost]
        public IActionResult Upsert(ProductViewModel productView, IFormFile? file)
        {
            // Add our own custom checks if any

            if (ModelState.IsValid)  // checks that inputs are valid according to annotations on the model we are working with
            {
                _uow.Products.ExecuteSql(@"
                        INSERT INTO dbo.Products (Title, Description, ISBN, Author, ListPrice, Price, Price50, Price100, CategoryId)
                        VALUES (@Title, @Description, @ISBN, @Author, @ListPrice, @Price, @Price50, @Price100, @CategoryId);
                    ", [
                            new SqlParameter("Title", productView.Product.Title),
                            new SqlParameter("Description", productView.Product.Description),
                            new SqlParameter("ISBN", productView.Product.ISBN),
                            new SqlParameter("Author", productView.Product.Author),
                            new SqlParameter("ListPrice", productView.Product.ListPrice),
                            new SqlParameter("Price", productView.Product.Price),
                            new SqlParameter("Price50", productView.Product.Price50),
                            new SqlParameter("Price100", productView.Product.Price100),
                            new SqlParameter("CategoryId", productView.Product.CategoryId)
                        ]);
                TempData["success"] = "Product created successfully"; // used for passing data in the next rendered page.
                return RedirectToAction("Index", "Product"); // redirects to the specified ACTION of secified CONTROLLER
            }

            IEnumerable<SelectListItem> CategoryList =
                _uow.Categories
                .FromSql(@$"
                    SELECT * 
                    FROM dbo.Categories;
                ", [])
                .Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() });

            productView.CategoryList = CategoryList;

            return View(productView);
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
            if (entityId is null || entityId == 0) return NotFound();

            var product = _uow.Products
                .FromSql($@"
                    SELECT * FROM dbo.Products
                    WHERE Id = @Id
                ", [new SqlParameter("Id", entityId)]).FirstOrDefault();

            if (product is null) return NotFound();

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
            if (entityId is null || entityId == 0) return NotFound();

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
