using System.Security.Claims;
using System.Text;
using Bookstore.Models.Domain;
using Bookstore.Models.ViewModels;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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
            var scv = GetShoppingCart();
            return scv is not null ? View(scv) : NotFound();
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
            var ShoppingCartView = GetShoppingCart();

            if (ShoppingCartView is null) return NotFound();

            if (ShoppingCartView.ShoppingCartList.ToList().Count == 0) return RedirectToAction(nameof(Index), "Cart");

            if (ShoppingCartView.OrderHeader?.ApplicationUser is not null)
            {
                ShoppingCartView.OrderHeader.Name = ShoppingCartView.OrderHeader.ApplicationUser.Name;
                ShoppingCartView.OrderHeader.PhoneNumber = ShoppingCartView.OrderHeader.ApplicationUser.PhoneNumber;
                ShoppingCartView.OrderHeader.StreetAddress = ShoppingCartView.OrderHeader.ApplicationUser.StreetAddress;
                ShoppingCartView.OrderHeader.City = ShoppingCartView.OrderHeader.ApplicationUser.City;
                ShoppingCartView.OrderHeader.State = ShoppingCartView.OrderHeader.ApplicationUser.State;
                ShoppingCartView.OrderHeader.PostalCode = ShoppingCartView.OrderHeader.ApplicationUser.PostalCode;
            }

            return View(ShoppingCartView);
        }

        [HttpPost]
        public IActionResult Summary(ShoppingCartViewModel orderSummaryView)
        {
            var OrderSummary = GetShoppingCart();

            if (OrderSummary?.ShoppingCartList.ToList().Count == 0) return View(OrderSummary);

            if (OrderSummary is null || OrderSummary.OrderHeader is null) return NotFound();

            ModelState.Remove("ShoppingCartList");
            ModelState.Remove("OrderHeader.ApplicationUserId");
            if (!ModelState.IsValid) return View(OrderSummary);

            if (orderSummaryView.OrderHeader is not null)
            {
                OrderSummary.OrderHeader.Name = orderSummaryView.OrderHeader.Name;
                OrderSummary.OrderHeader.PhoneNumber = orderSummaryView.OrderHeader.PhoneNumber;
                OrderSummary.OrderHeader.StreetAddress = orderSummaryView.OrderHeader.StreetAddress;
                OrderSummary.OrderHeader.City = orderSummaryView.OrderHeader.City;
                OrderSummary.OrderHeader.State = orderSummaryView.OrderHeader.State;
                OrderSummary.OrderHeader.PostalCode = orderSummaryView.OrderHeader.PostalCode;
            }

            OrderSummary.OrderHeader.OrderDate = System.DateTime.Now;

            if (OrderSummary.OrderHeader.ApplicationUser?.CompanyId is null || OrderSummary.OrderHeader.ApplicationUser.CompanyId == 0)
            {
                // regular client so process payment immediately
                // workflow = 
                //   Makes Payment (status: Pending, Payment: Pending)-> 
                //   Order Confirmation (status: Approved, Payment: Approved) -> 
                //   Processing (status: Processing, Payment: Approved)-> 
                //   Shipped (status: Shipped, Payment: Approved)
                OrderSummary.OrderHeader.OrderStatus = SD.OrderStatusPending;
                OrderSummary.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            }
            else
            {
                // company client so process order immediately. Payment will be sent later
                // workflow = 
                //   Order Confirmation (status: Approved, Payment: ApprovedForDelayedPayment) -> 
                //   Processing (status: Processing, Payment" ApprovedForDelayedPayment)-> 
                //   Shipped (status: Shipped, Payment: ApprovedForDelayedPayment)
                //   Makes Payment (status: Shipped, Payment: Approved)-> 
                OrderSummary.OrderHeader.OrderStatus = SD.OrderStatusApproved;
                OrderSummary.OrderHeader.PaymentStatus = SD.PaymentStatusApprovedDelayedPayment;
            }

            using var transaction = _uow.Context().BeginTransaction();

            var OrderHeaderId = _uow.OrderHeaders.SqlQuery<int>($@"
                INSERT INTO dbo.OrderHeaders
                (
                    ApplicationUserId, 
                    OrderDate, 
                    OrderTotal, 
                    OrderStatus, 
                    PaymentStatus, 
                    Name,
                    PhoneNumber,
                    StreetAddress,
                    City,
                    State,
                    PostalCode
                ) OUTPUT INSERTED.Id VALUES (
                    @ApplicationUserId, 
                    @OrderDate, 
                    @OrderTotal, 
                    @OrderStatus, 
                    @PaymentStatus, 
                    @Name,
                    @PhoneNumber,
                    @StreetAddress,
                    @City,
                    @State,
                    @PostalCode                    
                )
            ", [
                new SqlParameter("ApplicationUserId",OrderSummary.OrderHeader.ApplicationUserId),
                new SqlParameter("OrderDate",OrderSummary.OrderHeader.OrderDate),
                new SqlParameter("OrderTotal",OrderSummary.OrderHeader.OrderTotal),
                new SqlParameter("OrderStatus",OrderSummary.OrderHeader.OrderStatus),
                new SqlParameter("PaymentStatus",OrderSummary.OrderHeader.PaymentStatus),
                new SqlParameter("Name",OrderSummary.OrderHeader.Name),
                new SqlParameter("PhoneNumber",OrderSummary.OrderHeader.PhoneNumber),
                new SqlParameter("StreetAddress",OrderSummary.OrderHeader.StreetAddress),
                new SqlParameter("City",OrderSummary.OrderHeader.City),
                new SqlParameter("State",OrderSummary.OrderHeader.State),
                new SqlParameter("PostalCode",OrderSummary.OrderHeader.PostalCode)
            ])?.FirstOrDefault();

            // var OrderHeaderId = _uow.OrderHeaders.SqlQuery<decimal>($@"SELECT SCOPE_IDENTITY()", [])?.FirstOrDefault();
            if (OrderHeaderId != 0)
            {
                var inParams = new StringBuilder();
                var sqlParams = new List<SqlParameter>();
                var delInParams = new StringBuilder();
                var delSqlParams = new List<SqlParameter>();
                foreach (var (sc, idx) in OrderSummary.ShoppingCartList.Select((sc, idx) => (sc, idx)))
                {
                    inParams.Append($"(@OrderHeaderId{idx}, @ProductId{idx}, @Count{idx}, @Price{idx}),");
                    sqlParams.Add(new SqlParameter($"OrderHeaderId{idx}", OrderHeaderId));
                    sqlParams.Add(new SqlParameter($"ProductId{idx}", sc.ProductId));
                    sqlParams.Add(new SqlParameter($"Count{idx}", sc.Count));
                    sqlParams.Add(new SqlParameter($"Price{idx}", GetPriceBasedOnQuantity(sc)));

                    delInParams.Append($"@Id{idx},");
                    delSqlParams.Add(new SqlParameter($"Id{idx}", sc.Id));
                }

                _uow.OrderDetails.ExecuteSql($@"
                    INSERT INTO dbo.OrderDetails
                    (OrderHeaderId, ProductId, Count, Price)
                    VALUES {inParams.ToString()[..^1]}
                ", sqlParams);

                _uow.ShoppingCarts.ExecuteSql($@"
                    DELETE FROM dbo.ShoppingCarts
                    WHERE Id IN ({delInParams.ToString()[..^1]}) 
                ", delSqlParams);

                transaction.Commit();

                if (OrderSummary.OrderHeader.ApplicationUser?.CompanyId.GetValueOrDefault() == 0)
                {
                    // TODO: collect payment via stripe
                }
            }
            else
            {
                transaction.Rollback();
                return View(OrderSummary);
                // TODO: return some error page here.
            }

            return RedirectToAction(nameof(OrderConfirmation), "Cart", new { entityId = OrderHeaderId });
        }

        public IActionResult OrderConfirmation(int entityId)
        {
            return View(entityId);
        }

        private ShoppingCartViewModel? GetShoppingCart()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId is null) return null;

            var shoppingCartList = _uow.ShoppingCarts.FromSql($@"
                SELECT * 
                FROM ShoppingCarts
                WHERE (ApplicationUserId = @ApplicationUserId)
            ", [new SqlParameter("ApplicationUserId", userId)]);

            if (shoppingCartList is null || shoppingCartList.ToList().Count == 0)
            {
                return new ShoppingCartViewModel
                {
                    OrderHeader = new OrderHeader
                    {
                        ApplicationUserId = userId,
                        OrderTotal = 0
                    },
                    ShoppingCartList = []
                };
            }

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

            var OrderTotal = shoppingCartList.Select(sc => GetPriceBasedOnQuantity(sc) * sc.Count).Sum();

            var ApplicationUser = _uow.ApplicationUsers.FromSql($@"
                SELECT * FROM dbo.AspNetUsers WHERE Id = @Id
            ", [new SqlParameter("Id", userId)]).FirstOrDefault();

            var ShoppingCartView = new ShoppingCartViewModel
            {
                ShoppingCartList = shoppingCartList,
                OrderHeader = new OrderHeader
                {
                    ApplicationUserId = userId,
                    ApplicationUser = ApplicationUser,
                    OrderTotal = OrderTotal
                }
            };

            return ShoppingCartView;
        }

        private static double GetPriceBasedOnQuantity(ShoppingCart sc)
        {
            var price = sc.Count > 100
                    ? sc.Product.Price100 : sc.Count > 50
                    ? sc.Product.Price50 : sc.Product.Price;
            return price;
        }
    }
}
