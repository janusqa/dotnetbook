using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Models;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Bookstore.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Bookstore.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace bookstore.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork uow)
    {
        _logger = logger;
        _uow = uow;
    }

    public IActionResult Index()
    {
        var productList = _uow.Products
            .SqlQuery<ProductWithCategory>(@$"
                SELECT 
                    p.*,
                    c.Name AS CategoryName,
                    c.DisplayOrder As CategoryDisplayOrder
                FROM dbo.Products p INNER JOIN dbo.Categories c
                ON (p.CategoryId = C.Id)
        ", [])?
        .Select(pwc =>
            new Product
            {
                Id = pwc.Id,
                Title = pwc.Title,
                Description = pwc.Description,
                ISBN = pwc.ISBN,
                Author = pwc.Author,
                ListPrice = pwc.ListPrice,
                Price = pwc.Price,
                Price50 = pwc.Price50,
                Price100 = pwc.Price100,
                ImageUrl = pwc.ImageUrl,
                CategoryId = pwc.CategoryId,
                Category = new Category { Id = pwc.CategoryId, Name = pwc.CategoryName, DisplayOrder = pwc.CategoryDisplayOrder }
            }
        ).ToList();

        return View(productList ?? []);
    }

    public IActionResult Details(int entityId)
    {
        var product = _uow.Products
        .SqlQuery<ProductWithCategory>(@$"
            SELECT 
                p.*,
                c.Name AS CategoryName,
                c.DisplayOrder As CategoryDisplayOrder
            FROM dbo.Products p INNER JOIN dbo.Categories c
            ON (p.CategoryId = C.Id)
            WHERE (p.Id = @Id);
        ", [new SqlParameter("Id", entityId)])?
        .Select(pwc =>
            new Product
            {
                Id = pwc.Id,
                Title = pwc.Title,
                Description = pwc.Description,
                ISBN = pwc.ISBN,
                Author = pwc.Author,
                ListPrice = pwc.ListPrice,
                Price = pwc.Price,
                Price50 = pwc.Price50,
                Price100 = pwc.Price100,
                ImageUrl = pwc.ImageUrl,
                CategoryId = pwc.CategoryId,
                Category = new Category { Id = pwc.CategoryId, Name = pwc.CategoryName, DisplayOrder = pwc.CategoryDisplayOrder }
            }
        ).FirstOrDefault();

        if (product is null) return NotFound();

        var cart = new ShoppingCart { Product = product, ProductId = product.Id, Count = 1 };

        return View(cart);
    }

    [HttpPost]
    [Authorize] // To save an item to shopping cart user must be logged in. Role does not matter. We just need a ApplicationUserId
    public IActionResult Details(ShoppingCart cart)
    {
        var claimsIdentity = User.Identity as ClaimsIdentity;
        var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        cart.ApplicationUserId = userId;

        ModelState.Remove("ApplicationUserId");
        ModelState.SetModelValue("ApplicationUserId", new ValueProviderResult(userId, CultureInfo.CurrentCulture));
        TryValidateModel(cart);

        if (ModelState.IsValid)
        {
            _uow.ShoppingCarts.ExecuteSql(@"
                MERGE INTO dbo.ShoppingCarts AS target
                USING 
                    (
                        VALUES (@Id, @ApplicationUserId, @ProductId, @Count)
                    ) AS source (Id, ApplicationUserId, ProductId, Count)
                ON (target.ApplicationUserId = source.ApplicationUserId AND target.ProductId = source.ProductId)
                WHEN MATCHED THEN
                    UPDATE SET 
                        target.Count = source.Count
                WHEN NOT MATCHED THEN
                    INSERT (ApplicationUserId, ProductId, Count) 
                        VALUES (source.ApplicationUserId, source.ProductId, source.Count);
            ", [
                    new SqlParameter("Id", cart.Id),
                    new SqlParameter("ApplicationUserId", cart.ApplicationUserId),
                    new SqlParameter("ProductId", cart.ProductId),
                    new SqlParameter("Count", cart.Count),
                ]);
            TempData["success"] = $"Shopping Cart updated successfully"; // used for passing data in the next rendered page.
            return RedirectToAction(nameof(Index), "Home"); // redirects to the specified ACTION of secified CONTROLLER
        }

        var product = _uow.Products
                .SqlQuery<ProductWithCategory>(@$"
                    SELECT 
                        p.*,
                        c.Name AS CategoryName,
                        c.DisplayOrder As CategoryDisplayOrder
                    FROM dbo.Products p INNER JOIN dbo.Categories c
                    ON (p.CategoryId = C.Id)
                    WHERE (p.Id = @Id);
                ", [new SqlParameter("Id", cart.ProductId)])?
                .Select(pwc =>
                    new Product
                    {
                        Id = pwc.Id,
                        Title = pwc.Title,
                        Description = pwc.Description,
                        ISBN = pwc.ISBN,
                        Author = pwc.Author,
                        ListPrice = pwc.ListPrice,
                        Price = pwc.Price,
                        Price50 = pwc.Price50,
                        Price100 = pwc.Price100,
                        ImageUrl = pwc.ImageUrl,
                        CategoryId = pwc.CategoryId,
                        Category = new Category { Id = pwc.CategoryId, Name = pwc.CategoryName, DisplayOrder = pwc.CategoryDisplayOrder }
                    }
                ).FirstOrDefault();

        if (product is null) return NotFound();

        cart.Product = product;

        return View(cart);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
