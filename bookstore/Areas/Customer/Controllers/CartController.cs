using System.Security.Claims;
using System.Text;
using Bookstore.Models.Domain;
using Bookstore.Models.ViewModels;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace bookstore.Areas.Customer.Controllers
{

    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _uow;
        // private ShoppingCartViewModel? ShoppingCartView { get; set; }

        public CartController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IActionResult Index()
        {
            return View(GetShoppingCartViewModel());
        }

        public IActionResult Increase(int entityId)
        {
            _uow.ShoppingCarts.ExecuteSql($@"
                UPDATE dbo.ShoppingCarts SET Count = Count + 1 WHERE (Id = @Id) AND Count < 1001;
            ", [
                new SqlParameter("Id", entityId),
            ]);

            return RedirectToAction(nameof(Index), "Cart");
        }

        public IActionResult Decrease(int entityId)
        {
            _uow.ShoppingCarts.ExecuteSql($@"
                UPDATE dbo.ShoppingCarts SET Count = Count - 1 WHERE (Id = @Id) AND (Count > 1);
            ", [
                new SqlParameter("Id", entityId),
            ]);

            return RedirectToAction(nameof(Index), "Cart");
        }

        public IActionResult Remove(int entityId)
        {

            _uow.ShoppingCarts.ExecuteSql($@"
                DELETE FROM dbo.ShoppingCarts WHERE (Id = @Id);
            ", [
                new SqlParameter("Id", entityId),
            ]);

            return RedirectToAction(nameof(Index), "Cart");
        }

        public IActionResult Summary()
        {
            var ShoppingCartView = GetShoppingCartViewModel();

            ShoppingCartView.OrderHeader.ApplicationUser = _uow.ApplicationUsers.FromSql($@"
                SELECT * FROM dbo.AspNetUsers WHERE Id = @Id
            ", [new SqlParameter("Id", ShoppingCartView.OrderHeader.ApplicationUserId)]).FirstOrDefault();

            ShoppingCartView.OrderHeader.Name = ShoppingCartView.OrderHeader.ApplicationUser?.Name;
            ShoppingCartView.OrderHeader.PhoneNumber = ShoppingCartView.OrderHeader.ApplicationUser?.PhoneNumber;
            ShoppingCartView.OrderHeader.StreetAddress = ShoppingCartView.OrderHeader.ApplicationUser?.StreetAddress;
            ShoppingCartView.OrderHeader.City = ShoppingCartView.OrderHeader.ApplicationUser?.City;
            ShoppingCartView.OrderHeader.State = ShoppingCartView.OrderHeader.ApplicationUser?.State;
            ShoppingCartView.OrderHeader.PostalCode = ShoppingCartView.OrderHeader.ApplicationUser?.PostalCode;

            return View(ShoppingCartView);
        }

        private ShoppingCartViewModel GetShoppingCartViewModel()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var shoppingCartList = _uow.ShoppingCarts.FromSql($@"
                SELECT * 
                FROM ShoppingCarts
                WHERE (ApplicationUserId = @ApplicationUserId)
            ", [new SqlParameter("ApplicationUserId", userId)]);

            var productIds = shoppingCartList.Select(sc => sc.ProductId).ToList();
            var inParams = new StringBuilder();
            var sqlParams = new List<SqlParameter>();
            foreach (var (pId, idx) in productIds.Select((pId, idx) => (pId, idx)))
            {
                inParams.Append($"@p{idx},");
                sqlParams.Add(new SqlParameter($"p{idx}", pId));
            }
            var products = _uow.Products.FromSql($@"
                SELECT * 
                FROM Products
                WHERE Id IN ({inParams.ToString()[..^1]});
            ", sqlParams).ToDictionary(p => p.Id, p => p);

            foreach (var sc in shoppingCartList)
            {
                sc.Product = products[sc.ProductId];
            };

            var OrderTotal = shoppingCartList.Select(GetPriceBasedOnQuantity).Sum();

            var ShoppingCartView = new ShoppingCartViewModel(shoppingCartList);
            ShoppingCartView.OrderHeader.OrderTotal = OrderTotal;
            ShoppingCartView.OrderHeader.ApplicationUserId = userId;

            return ShoppingCartView;
        }

        private static double GetPriceBasedOnQuantity(ShoppingCart sc)
        {
            var price = sc.Count > 100
                    ? sc.Product.Price100 : sc.Count > 50
                    ? sc.Product.Price50 : sc.Product.Price;
            return price * sc.Count;
        }

    }
}
