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
using Bookstore.Utility;

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

        return View(productList ?? []);
    }

    public IActionResult Details(int entityId)
    {
        var product = _uow.Products
        .SqlQuery<ProductWithIncluded>(@$"
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

            // we need to store the number of distince user items a user has in a session so we can display it in 
            // header of site.  We must make a database call to get the number of items in cart for this logged in user
            var itemCount = _uow.ShoppingCarts.SqlQuery<int>($@"
                SELECT COUNT(Id) FROM dbo.ShoppingCarts WHERE ApplicationUserId = @ApplicationUserId
            ", [new SqlParameter("ApplicationUserId", cart.ApplicationUserId)])?.FirstOrDefault();
            HttpContext.Session.SetInt32(SD.SessionCart, itemCount ?? 0);

            TempData["success"] = $"Shopping Cart updated successfully"; // used for passing data in the next rendered page.
            return RedirectToAction(nameof(Index), "Home"); // redirects to the specified ACTION of secified CONTROLLER
        }

        // if adding to cart fields we need to retrieve product info again to display on details
        // because this detail product information would not be available otherwise when the page reloads
        // after fail vailidation.
        var product = _uow.Products
                .SqlQuery<ProductWithIncluded>(@$"
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
