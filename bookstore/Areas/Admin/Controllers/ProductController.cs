using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Bookstore.DataAccess.Data;
using Bookstore.Models.Domain;
using Microsoft.IdentityModel.Tokens;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bookstore.Models.ViewModels;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Bookstore.Utility;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace bookstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvrionment; // for handling file uploads
        private readonly IUnitOfWork _uow;
        // pass in our db context to construtor of controller
        // We now have access to the db via this.
        // Recall we already have all this configured in the services
        // area of "Program.cs"
        public ProductController(IUnitOfWork uow, IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvrionment = webHostEnvironment;
            _uow = uow;
        }

        // NOTE: we originally we had a mvc delete controller with view
        // We now use the webapi endpoint at the bottom so this is deprecated
        // We still use it but only to load the JS that grabs the products using 
        // the webapi.  So when we return we do not return a viewmodel.
        public IActionResult Index()
        {
            var productList = _uow.Products
                .SqlQuery<ProductWithIncluded>(@$"
                SELECT 
                    p.*,
                    c.Name AS CategoryName,
                    c.DisplayOrder As CategoryDisplayOrder,
                    pi.Id AS ProductImageId,
                    pi.ImageUrl as ProductImageUrl
                FROM dbo.Products p INNER JOIN dbo.Categories c ON (p.CategoryId = C.Id)
                LEFT JOIN dbo.ProductImages pi ON (pi.ProductId = p.Id)
            ", [])?
            .GroupBy(
                p => p.Id,
                p => new
                {
                    p.Title,
                    p.Description,
                    p.ISBN,
                    p.Author,
                    p.ListPrice,
                    p.Price,
                    p.Price50,
                    p.Price100,
                    p.CategoryId,
                    p.CategoryName,
                    p.CategoryDisplayOrder,
                    p.ProductImageId,
                    p.ProductImageUrl
                },
                (k, g) => new Product
                {
                    Id = k,
                    Title = g.First().Title,
                    Description = g.First().Description,
                    ISBN = g.First().ISBN,
                    Author = g.First().Author,
                    ListPrice = g.First().ListPrice,
                    Price = g.First().Price,
                    Price50 = g.First().Price50,
                    Price100 = g.First().Price100,
                    CategoryId = g.First().CategoryId,
                    Category = new Category { Id = g.First().CategoryId, Name = g.First().CategoryName, DisplayOrder = g.First().CategoryDisplayOrder },
                    ProductImages = g
                        .Where(p => p.ProductImageId is not null)
                        .Select(p => new ProductImage { Id = p.ProductImageId ?? 0, ImageUrl = p.ProductImageUrl ?? "", ProductId = k }).ToList()

                }
                ).ToList() ?? [];

            // we are not actually using the query view model retrieved above anymore.
            // So we can return a View with no view model.  The View cshtml is now populated
            // from the GetAll WebApi at the bottom in WebApi region part.
            // return View(productList ?? []);
            return View();
        }

        // for Create/Edit for product we will handle it differently to Category.
        // We will use one View and One controller named Upsert that will handle
        // The fuctionaility of both creating and updating.
        public IActionResult Upsert(int? entityId)
        {
            var product = entityId is null || entityId == 0
                ? new Product()
                : _uow.Products.SqlQuery<ProductWithIncluded>(@$"
                SELECT 
                    p.*,
                    c.Name AS CategoryName,
                    c.DisplayOrder As CategoryDisplayOrder,
                    pi.Id AS ProductImageId,
                    pi.ImageUrl as ProductImageUrl
                FROM dbo.Products p INNER JOIN dbo.Categories c ON (p.CategoryId = C.Id)
                LEFT JOIN dbo.ProductImages pi ON (pi.ProductId = p.Id)
                WHERE p.Id = @Id
            ", [new SqlParameter("Id", entityId)])?
            .GroupBy(
                p => p.Id,
                p => new
                {
                    p.Title,
                    p.Description,
                    p.ISBN,
                    p.Author,
                    p.ListPrice,
                    p.Price,
                    p.Price50,
                    p.Price100,
                    p.CategoryId,
                    p.CategoryName,
                    p.CategoryDisplayOrder,
                    p.ProductImageId,
                    p.ProductImageUrl
                },
                (k, g) => new Product
                {
                    Id = k,
                    Title = g.First().Title,
                    Description = g.First().Description,
                    ISBN = g.First().ISBN,
                    Author = g.First().Author,
                    ListPrice = g.First().ListPrice,
                    Price = g.First().Price,
                    Price50 = g.First().Price50,
                    Price100 = g.First().Price100,
                    CategoryId = g.First().CategoryId,
                    Category = new Category { Id = g.First().CategoryId, Name = g.First().CategoryName, DisplayOrder = g.First().CategoryDisplayOrder },
                    ProductImages = g
                        .Where(p => p.ProductImageId is not null)
                        .Select(p => new ProductImage { Id = p.ProductImageId ?? 0, ImageUrl = p.ProductImageUrl ?? "", ProductId = k }).ToList()
                }
                ).ToList().FirstOrDefault();

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
        public IActionResult Upsert(ProductViewModel productView, List<IFormFile>? files)
        {
            // Add our own custom checks if any

            if (ModelState.IsValid)  // checks that inputs are valid according to annotations on the model we are working with
            {
                var productId = _uow.Products.SqlQuery<int>(@"
                        MERGE INTO dbo.Products AS target
                        USING 
                            (
                                VALUES (@Id, @Title, @Description, @ISBN, @Author, @ListPrice, @Price, @Price50, @Price100, @CategoryId)
                            ) AS source (Id, Title, Description, ISBN, Author, ListPrice, Price, Price50, Price100, CategoryId)
                        ON target.Id = source.Id
                        WHEN MATCHED THEN
                            UPDATE SET 
                                target.Title = source.Title, 
                                target.Description = source.Description, 
                                target.ISBN = source.ISBN, 
                                target.Author = source.Author, 
                                target.ListPrice = source.ListPrice, 
                                target.Price = source.Price, 
                                target.Price50 = source.Price50, 
                                target.Price100 = source.Price100,
                                target.CategoryId = source.CategoryId
                        WHEN NOT MATCHED THEN
                            INSERT (Title, Description, ISBN, Author, ListPrice, Price, Price50, Price100, CategoryId) 
                                    VALUES (source.Title, source.Description, source.ISBN, source.Author, source.ListPrice, source.Price, source.Price50, source.Price100, source.CategoryId)
                        OUTPUT inserted.Id;
                    ", [
                            new SqlParameter("Id", productView.Product.Id),
                            new SqlParameter("Title", productView.Product.Title),
                            new SqlParameter("Description", productView.Product.Description),
                            new SqlParameter("ISBN", productView.Product.ISBN),
                            new SqlParameter("Author", productView.Product.Author),
                            new SqlParameter("ListPrice", productView.Product.ListPrice),
                            new SqlParameter("Price", productView.Product.Price),
                            new SqlParameter("Price50", productView.Product.Price50),
                            new SqlParameter("Price100", productView.Product.Price100),
                            new SqlParameter("CategoryId", productView.Product.CategoryId),
                    ])?.FirstOrDefault();

                if (files is not null && files.Count > 0 && productId is not null)
                {
                    string wwwRootPath = _webHostEnvrionment.WebRootPath;
                    var inParams = new StringBuilder();
                    var sqlParams = new List<SqlParameter> { new SqlParameter($"pid", productId) };
                    foreach (var (file, idx) in files.Select((file, idx) => (file, idx)))
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        string urlPath = $@"images/product/product_{productId}";
                        string filePath = Path.Combine(wwwRootPath, urlPath);


                        if (!Directory.Exists(filePath))
                        {
                            Directory.CreateDirectory(filePath);
                        }

                        using (FileStream writer = new FileStream(Path.Combine(filePath, fileName), FileMode.Create))
                        {
                            file.CopyTo(writer);
                        }

                        inParams.Append($"(@iu{idx}, @pid),");
                        sqlParams.Add(new SqlParameter($"iu{idx}", $@"/{urlPath}/{fileName}"));
                    }

                    _uow.ProductImages.ExecuteSql($@"
                        INSERT INTO dbo.ProductImages
                        (ImageUrl, ProductId) VALUES {inParams.ToString()[..^1]}
                    ", sqlParams);
                }

                var productImages = _uow.ProductImages.FromSql($@"
                    SELECT * FROM dbo.ProductImages WHERE ProductId = @ProductId
                ", [new SqlParameter("ProductId", productId)]);
                productView.Product.ProductImages = productImages.ToList();

                TempData["success"] = $"Product {(productView.Product.Id == 0 ? "created" : "updated")} successfully"; // used for passing data in the next rendered page.
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

        // Derprecated. Not used. Kept for demo. Use multi file upload controller above.
        [HttpPost]
        public IActionResult UpsertSingleImageUploadImplementation(ProductViewModel productView, IFormFile? file)
        {
            // Add our own custom checks if any

            if (ModelState.IsValid)  // checks that inputs are valid according to annotations on the model we are working with
            {
                if (file is not null)
                {
                    string wwwRootPath = _webHostEnvrionment.WebRootPath;
                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    string urlPath = @"images/product";
                    string filePath = Path.Combine(wwwRootPath, urlPath);

                    // if a file was uploaded and there is an existing file
                    // we need to repalce the existing file by first deleting 
                    // it and then copying in the new file. Otherwise, just
                    // copy in the new file
                    if (productView.Product.ImageUrl is not null && productView.Product.ImageUrl != "")
                    {
                        var existingImage = Path.Combine(wwwRootPath, productView.Product.ImageUrl[1..]);
                        if (System.IO.File.Exists(existingImage)) System.IO.File.Delete(existingImage);
                    }

                    using (FileStream writer = new FileStream(Path.Combine(filePath, fileName), FileMode.Create))
                    {
                        file.CopyTo(writer);
                    }
                    productView.Product.ImageUrl = @$"/{urlPath}/{fileName}";
                }
                else
                {
                    // if no file is uploaded but a file is already in the database
                    // keep that file, otherwise set ImageUrl to empty string.
                    productView.Product.ImageUrl =
                        productView.Product.ImageUrl is null || productView.Product.ImageUrl == ""
                        ? ""
                        : productView.Product.ImageUrl;
                }

                _uow.Products.ExecuteSql(@"
                        MERGE INTO dbo.Products AS target
                        USING 
                            (
                                VALUES (@Id, @Title, @Description, @ISBN, @Author, @ListPrice, @Price, @Price50, @Price100, @CategoryId, @ImageUrl)
                            ) AS source (Id, Title, Description, ISBN, Author, ListPrice, Price, Price50, Price100, CategoryId, ImageUrl)
                        ON target.Id = source.Id
                        WHEN MATCHED THEN
                            UPDATE SET 
                                target.Title = source.Title, 
                                target.Description = source.Description, 
                                target.ISBN = source.ISBN, 
                                target.Author = source.Author, 
                                target.ListPrice = source.ListPrice, 
                                target.Price = source.Price, 
                                target.Price50 = source.Price50, 
                                target.Price100 = source.Price100,
                                target.CategoryId = source.CategoryId,
                                target.ImageUrl = source.ImageUrl
                        WHEN NOT MATCHED THEN
                            INSERT (Title, Description, ISBN, Author, ListPrice, Price, Price50, Price100, CategoryId, ImageUrl) 
                                    VALUES (source.Title, source.Description, source.ISBN, source.Author, source.ListPrice, source.Price, source.Price50, source.Price100, source.CategoryId, source.ImageUrl);
                    ", [
                            new SqlParameter("Id", productView.Product.Id),
                            new SqlParameter("Title", productView.Product.Title),
                            new SqlParameter("Description", productView.Product.Description),
                            new SqlParameter("ISBN", productView.Product.ISBN),
                            new SqlParameter("Author", productView.Product.Author),
                            new SqlParameter("ListPrice", productView.Product.ListPrice),
                            new SqlParameter("Price", productView.Product.Price),
                            new SqlParameter("Price50", productView.Product.Price50),
                            new SqlParameter("Price100", productView.Product.Price100),
                            new SqlParameter("CategoryId", productView.Product.CategoryId),
                            new SqlParameter("ImageUrl", productView.Product.ImageUrl)
                    ]);
                TempData["success"] = $"Product {(productView.Product.Id == 0 ? "created" : "updated")} successfully"; // used for passing data in the next rendered page.
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

        // NOTE: we originally we had a mvc delete controller with view
        // we are now using this api to delete without showing a view.
        // so that controller is now deprecated and of no use. 
        // Keeping it around as just an example of a controller
        // that means the controller below both of them are no
        // in use. They are depracated in favor of DeleteEntity 
        // API in the regions at end of this page.
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

        // NOTE: we originally we had a mvc delete controller with view
        // we are now using this api to delete without showing a view.
        // so that controller is now deprecated and of no use. 
        //
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

        public IActionResult DeleteImage(int entityId)
        {
            var ImageToDelete = _uow.ProductImages.FromSql(@$"
                    SELECT 
                        *
                    FROM dbo.ProductImages
                    WHERE (Id = @Id);
                ", [new SqlParameter("Id", entityId)])?
                .FirstOrDefault();

            if (ImageToDelete is not null)
            {
                string wwwRootPath = _webHostEnvrionment.WebRootPath;
                string existingImage = Path.Combine(wwwRootPath, ImageToDelete.ImageUrl[1..]);
                if (System.IO.File.Exists(existingImage)) System.IO.File.Delete(existingImage);
            }

            _uow.ProductImages.ExecuteSql($@"
                DELETE FROM dbo.ProductImages WHERE Id = @Id
            ", [new SqlParameter("Id", entityId)]);

            TempData["success"] = "Image deleted successfully";

            return RedirectToAction(nameof(Upsert), "Product", new { entityId = ImageToDelete?.ProductId });
        }

        // Note this we use this now instead of above mvc controller.
        // It is used to populate the datatables.net table
        // we can define WebApi calls directly in mvc by creating a region
        // use WebAPI annotations
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            var productList = _uow.Products.SqlQuery<ProductWithIncluded>(@$"
                SELECT 
                    p.*,
                    c.Name AS CategoryName,
                    c.DisplayOrder As CategoryDisplayOrder,
                    pi.Id AS ProductImageId,
                    pi.ImageUrl as ProductImageUrl
                FROM dbo.Products p INNER JOIN dbo.Categories c ON (p.CategoryId = C.Id)
                LEFT JOIN dbo.ProductImages pi ON (pi.ProductId = p.Id)
            ", [])?
            .GroupBy(
                p => p.Id,
                p => new
                {
                    p.Title,
                    p.Description,
                    p.ISBN,
                    p.Author,
                    p.ListPrice,
                    p.Price,
                    p.Price50,
                    p.Price100,
                    p.CategoryId,
                    p.CategoryName,
                    p.CategoryDisplayOrder,
                    p.ProductImageId,
                    p.ProductImageUrl
                },
                (k, g) => new Product
                {
                    Id = k,
                    Title = g.First().Title,
                    Description = g.First().Description,
                    ISBN = g.First().ISBN,
                    Author = g.First().Author,
                    ListPrice = g.First().ListPrice,
                    Price = g.First().Price,
                    Price50 = g.First().Price50,
                    Price100 = g.First().Price100,
                    CategoryId = g.First().CategoryId,
                    Category = new Category { Id = g.First().CategoryId, Name = g.First().CategoryName, DisplayOrder = g.First().CategoryDisplayOrder },
                    ProductImages = g
                        .Where(p => p.ProductImageId is not null)
                        .Select(p => new ProductImage { Id = p.ProductImageId ?? 0, ImageUrl = p.ProductImageUrl ?? "", ProductId = k }).ToList()
                }
                ).ToList() ?? [];

            return Json(new { data = productList ?? [] });
        }

        // NOTE: we originally we had a mvc delete controller with view
        // we are now using this api to delete without showing a view.
        // so that controller is now deprecated and of no use. 
        // Keeping it around as just an example of a controller
        [HttpDelete]
        public IActionResult Delete([FromBody] JsonElement requestBody)
        {
            int? entityId = requestBody.GetProperty("entityId").GetInt32();
            if (entityId is null || entityId == 0) return Json(new { success = false, message = "Not found" });

            // Old code for when only single file upload was deleted
            // and when we stored the image in the product table.
            //
            // var ImageUrlToDelete = _uow.Products.SqlQuery<string>(@$"
            //         SELECT 
            //             ImageUrl
            //         FROM dbo.Products
            //         WHERE (Id = @Id);
            //     ", [new SqlParameter("Id", entityId)])?
            // .FirstOrDefault();

            // if (ImageUrlToDelete is not null && ImageUrlToDelete != "")
            // {
            //     string wwwRootPath = _webHostEnvrionment.WebRootPath;
            //     string existingImage = Path.Combine(wwwRootPath, ImageUrlToDelete[1..]);
            //     if (System.IO.File.Exists(existingImage)) System.IO.File.Delete(existingImage);
            // }

            string wwwRootPath = _webHostEnvrionment.WebRootPath;
            string urlPath = $@"images/product/product_{entityId}";
            string filePath = Path.Combine(wwwRootPath, urlPath);

            if (Directory.Exists(filePath))
            {
                // Deleting files individually not needed, we can pass
                // second arg to Directory.Delete to delete recursively
                // 
                // string[] files = Directory.GetFiles(filePath);
                // foreach (var file in files)
                // {
                //     System.IO.File.Delete(file);
                // }
                Directory.Delete(filePath, true);
            }

            _uow.Products.ExecuteSql($@"
                    DELETE FROM dbo.Products  WHERE Id = @Id;
                ", [new SqlParameter("Id", entityId)]);

            return Json(new { success = true, message = "Delete successful" });

        }
        #endregion

    }
}
