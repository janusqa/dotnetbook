using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Bookstore.Models;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Bookstore.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Bookstore.Models.Domain;

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

        return View(product);
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
