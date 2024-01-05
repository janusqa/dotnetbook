using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using bookstore.Data;
using bookstore.Models;

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
            Console.WriteLine(string.Join(", ", categoryList.Select(c => c.Name)));
            return View(categoryList);
        }

        public IActionResult Create()
        {
            return View();
        }
    }
}
