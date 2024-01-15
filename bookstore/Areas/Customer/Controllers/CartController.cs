using System.Security.Claims;
using System.Text;
using Bookstore.Models.Domain;
using Bookstore.Models.Identity;
using Bookstore.Models.ViewModels;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Stripe.BillingPortal;
using Stripe.Checkout;

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

                foreach (var (sc, idx) in OrderSummary.ShoppingCartList.Select((sc, idx) => (sc, idx)))
                {
                    inParams.Append($"(@OrderHeaderId{idx}, @ProductId{idx}, @Count{idx}, @Price{idx}),");
                    sqlParams.Add(new SqlParameter($"OrderHeaderId{idx}", OrderHeaderId));
                    sqlParams.Add(new SqlParameter($"ProductId{idx}", sc.ProductId));
                    sqlParams.Add(new SqlParameter($"Count{idx}", sc.Count));
                    sqlParams.Add(new SqlParameter($"Price{idx}", GetPriceBasedOnQuantity(sc)));
                }

                _uow.OrderDetails.ExecuteSql($@"
                    INSERT INTO dbo.OrderDetails
                    (OrderHeaderId, ProductId, Count, Price)
                    VALUES {inParams.ToString()[..^1]}
                ", sqlParams);

                if (OrderSummary.OrderHeader.ApplicationUser?.CompanyId.GetValueOrDefault() == 0)
                {
                    // This is a non-company transaction so beging payment processing
                    // if it fails rollback, otherwise commit the order
                    var baseUrl = @"https://localhost:7125";

                    // Stripe
                    var options = new Stripe.Checkout.SessionCreateOptions
                    {
                        SuccessUrl = $"{baseUrl}/Customer/Cart/OrderConfirmation?entityId={OrderHeaderId}",
                        CancelUrl = $"{baseUrl}/Customer/Cart/Index",
                        LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                        Mode = "payment",
                    };
                    foreach (var item in OrderSummary.ShoppingCartList)
                    {
                        options.LineItems.Add(
                            new Stripe.Checkout.SessionLineItemOptions
                            {
                                PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                                {
                                    UnitAmount = (long)(GetPriceBasedOnQuantity(item) * 100),
                                    Currency = "usd",
                                    ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                                    {
                                        Name = item.Product.Title
                                    }
                                },
                                Quantity = item.Count
                            }
                        );
                    }

                    var service = new Stripe.Checkout.SessionService();
                    var session = service.Create(options);

                    if (session is not null)
                    {
                        _uow.OrderHeaders.ExecuteSql($@"
                            UPDATE dbo.OrderHeaders
                            SET 
                                SessionId = @SessionId
                            WHERE Id = @Id
                        ", [
                            new SqlParameter("Id", OrderHeaderId),
                            new SqlParameter("SessionId", session.Id),
                        ]);

                        transaction.Commit();

                        Response.Headers.Append("Location", session.Url);
                        return new StatusCodeResult(303); // redirect to stipe to process payment
                    }
                    else
                    {
                        transaction.Rollback();
                    }
                }
                else
                {
                    // This is a Company order so we do not try to get payment immediately
                    // log purchase order and proceed
                    transaction.Commit();
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
            var orderHeader = _uow.OrderHeaders.FromSql($@"
                SELECT * FROM dbo.OrderHeaders WHERE Id = @Id
            ", [new SqlParameter("Id", entityId)]).FirstOrDefault();

            if (orderHeader is not null)
            {
                using var transaction = _uow.Context().BeginTransaction();

                // On a successful order clear cart
                var order = GetShoppingCart();
                if (order is not null)
                {
                    var inParams = new StringBuilder();
                    var sqlParams = new List<SqlParameter>();
                    foreach (var (sc, idx) in order.ShoppingCartList.Select((sc, idx) => (sc, idx)))
                    {
                        inParams.Append($"@Id{idx},");
                        sqlParams.Add(new SqlParameter($"Id{idx}", sc.Id));
                    }

                    _uow.ShoppingCarts.ExecuteSql($@"
                            DELETE FROM dbo.ShoppingCarts
                            WHERE Id IN ({inParams.ToString()[..^1]}) 
                        ", sqlParams);
                }

                if (orderHeader.PaymentStatus != SD.PaymentStatusApprovedDelayedPayment)
                {
                    // we need to complete processing of a customer/stripe payment
                    var service = new Stripe.Checkout.SessionService();
                    var session = service.Get(orderHeader.SessionId);
                    if (session.PaymentStatus.Equals("paid", StringComparison.CurrentCultureIgnoreCase))
                    {
                        _uow.OrderHeaders.ExecuteSql($@"
                            UPDATE dbo.OrderHeaders
                            SET 
                                SessionId = @SessionId,
                                PaymentIntentId = @PaymentIntentId,
                                PaymentStatus = @PaymentStatus,
                                OrderStatus = @OrderStatus,
                                PaymentDate = @PaymentDate
                            WHERE Id = @Id
                        ", [
                            new SqlParameter("Id", entityId),
                            new SqlParameter("SessionId", session.Id),
                            new SqlParameter("PaymentIntentId", session.PaymentIntentId),
                            new SqlParameter("PaymentStatus", SD.PaymentStatusApproved),
                            new SqlParameter("OrderStatus", SD.OrderStatusApproved),
                            new SqlParameter("PaymentDate", DateTime.Now),
                        ]);

                        transaction.Commit();
                    }
                    else
                    {
                        transaction.Rollback();
                        // TODO: display some kind of failed transaction message
                    }
                }
            }
            else
            {
                // TODO: display some kind of failed transaction message
            }

            return View(entityId);
        }

        private ShoppingCartViewModel? GetShoppingCart()
        {
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId is null) return null;

            var applicationUser = _uow.ApplicationUsers.FromSql($@"
                SELECT * FROM dbo.AspNetUsers WHERE Id = @Id
            ", [new SqlParameter("Id", userId)]).FirstOrDefault();

            var shoppingCartList = _uow.ShoppingCarts.FromSql($@"
                SELECT * 
                FROM dbo.ShoppingCarts
                WHERE (ApplicationUserId = @ApplicationUserId)
            ", [new SqlParameter("ApplicationUserId", userId)]);

            if (shoppingCartList is null || shoppingCartList.ToList().Count == 0)
            {
                return new ShoppingCartViewModel
                {
                    OrderHeader = new OrderHeader
                    {
                        ApplicationUserId = userId,
                        ApplicationUser = applicationUser ?? new ApplicationUser(),
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
                FROM dbo.Products
                WHERE Id IN ({inParams.ToString()[..^1]});
            ", sqlParams).ToDictionary(p => p.Id, p => p);

            foreach (var sc in shoppingCartList)
            {
                sc.Product = products[sc.ProductId];
            };

            var OrderTotal = shoppingCartList.Select(sc => GetPriceBasedOnQuantity(sc) * sc.Count).Sum();

            var ShoppingCartView = new ShoppingCartViewModel
            {
                ShoppingCartList = shoppingCartList,
                OrderHeader = new OrderHeader
                {
                    ApplicationUserId = userId,
                    ApplicationUser = applicationUser ?? new ApplicationUser(),
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
