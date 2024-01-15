using System.Security.Claims;
using System.Text;
using Bookstore.Models.Domain;
using Bookstore.Models.ViewModels;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace bookstore.Areas.Admin.Controllers
{
    [Area("Admin")]
    // this protects this controller from unauthorized roles. 
    // If we need finer grained control we can apply it instead directly to actions in the controller
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _uow;

        public OrderController(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int entityId)
        {
            var orderHeader = _uow.OrderHeaders.FromSql(@$"
                SELECT * FROM dbo.OrderHeaders WHERE Id = @Id
            ", [new SqlParameter("Id", entityId)]).FirstOrDefault();

            if (orderHeader is null) return RedirectToAction(nameof(Index), "Order");

            var ApplicationUser = _uow.ApplicationUsers.FromSql($@"
                SELECT * FROM dbo.AspNetUsers WHERE Id = @Id
            ", [new SqlParameter("Id", orderHeader.ApplicationUserId)]).FirstOrDefault();
            if (ApplicationUser is not null) orderHeader.ApplicationUser = ApplicationUser;

            var orderDetails = _uow.OrderDetails.FromSql(@$"
                SELECT * FROM dbo.OrderDetails WHERE OrderHeaderId = @OrderHeaderId
            ", [new SqlParameter("OrderHeaderId", entityId)]).ToList();

            var productIds = orderDetails.Select(od => od.ProductId).ToList();
            var inParams = new StringBuilder();
            var sqlParams = new List<SqlParameter>();
            foreach (var (Id, idx) in productIds.Select((Id, idx) => (Id, idx)))
            {
                inParams.Append($"@p{idx},");
                sqlParams.Add(new SqlParameter($"p{idx}", Id));
            }
            var products = _uow.Products.FromSql($@"
                SELECT * FROM dbo.Products WHERE Id IN ({inParams.ToString()[..^1]});
            ", sqlParams).ToDictionary(p => p.Id, p => p);
            foreach (var od in orderDetails)
            {
                od.Product = products[od.ProductId];
            };

            var order = new OrderViewModel { OrderHeader = orderHeader, OrderDetails = orderDetails };

            return View(order);
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public IActionResult UpdateOrderDetails(OrderViewModel orderViewModel)
        {
            _uow.OrderHeaders.ExecuteSql($@"
                UPDATE dbo.OrderHeaders
                SET
                    Name = @Name,
                    PhoneNumber = @PhoneNumber,
                    StreetAddress = @StreetAddress,
                    City = @City,
                    State = @State,
                    PostalCode = @PostalCode,
                    Carrier = ISNULL(@Carrier, Carrier),
                    TrackingNumber = ISNULL(@TrackingNumber, TrackingNumber)
                WHERE (Id = @Id)
            ", [
                new SqlParameter("Id", orderViewModel.OrderHeader.Id),
                new SqlParameter("Name", orderViewModel.OrderHeader.Name),
                new SqlParameter("PhoneNumber", orderViewModel.OrderHeader.PhoneNumber),
                new SqlParameter("StreetAddress", orderViewModel.OrderHeader.StreetAddress),
                new SqlParameter("City", orderViewModel.OrderHeader.City),
                new SqlParameter("State", orderViewModel.OrderHeader.State),
                new SqlParameter("PostalCode", orderViewModel.OrderHeader.PostalCode),
                new SqlParameter("Carrier", orderViewModel.OrderHeader.Carrier ?? (object)DBNull.Value),
                new SqlParameter("TrackingNumber", orderViewModel.OrderHeader.TrackingNumber ?? (object)DBNull.Value),
            ]);

            TempData["success"] = "Order Details Updated Successfully";

            return RedirectToAction(nameof(Details), "Order", new { entityId = orderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public IActionResult StartProcessing(OrderViewModel orderViewModel)
        {
            _uow.OrderHeaders.ExecuteSql($@"
                UPDATE dbo.OrderHeaders
                SET
                    OrderStatus = @OrderStatus
                WHERE (Id = @Id)
            ", [
                new SqlParameter("Id", orderViewModel.OrderHeader.Id),
                new SqlParameter("OrderStatus", SD.OrderStatusInProcess)
            ]);

            TempData["success"] = "Order Details Updated Successfully";

            return RedirectToAction(nameof(Details), "Order", new { entityId = orderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public IActionResult ShipOrder(OrderViewModel orderViewModel)
        {
            if (orderViewModel.OrderHeader.Carrier is null)
            {
                TempData["error"] = "Carrier information is missing.";
                return RedirectToAction(nameof(Details), "Order", new { entityId = orderViewModel.OrderHeader.Id });
            }

            if (orderViewModel.OrderHeader.TrackingNumber is null)
            {
                TempData["error"] = "Tracking Number is missing";
                return RedirectToAction(nameof(Details), "Order", new { entityId = orderViewModel.OrderHeader.Id });
            }

            _uow.OrderHeaders.ExecuteSql($@"
                UPDATE dbo.OrderHeaders
                SET
                    OrderStatus = @OrderStatus,
                    ShippingDate = @ShippingDate,
                    Carrier = ISNULL(@Carrier, Carrier),
                    TrackingNumber = ISNULL(@TrackingNumber, TrackingNumber),
                    PaymentDueDate = ISNULL(@PaymentDueDate, PaymentDueDate)
                WHERE (Id = @Id)
            ", [
                new SqlParameter("Id", orderViewModel.OrderHeader.Id),
                new SqlParameter("OrderStatus", SD.OrderStatusShipped),
                new SqlParameter("ShippingDate", DateTime.Now),
                new SqlParameter("Carrier", orderViewModel.OrderHeader.Carrier ?? (object)DBNull.Value),
                new SqlParameter("TrackingNumber", orderViewModel.OrderHeader.TrackingNumber ?? (object)DBNull.Value),
                new SqlParameter("PaymentDueDate", orderViewModel.OrderHeader.PaymentStatus is not null &&
                    orderViewModel.OrderHeader.PaymentStatus.Equals(SD.PaymentStatusApprovedDelayedPayment, StringComparison.CurrentCultureIgnoreCase)
                        ? DateTime.Now.AddDays(30) : (object)DBNull.Value)
            ]);

            TempData["success"] = "Order Shipped Successfully";

            return RedirectToAction(nameof(Details), "Order", new { entityId = orderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
        public IActionResult CancelOrder(OrderViewModel orderViewModel)
        {
            var order = _uow.OrderHeaders.FromSql($@"
                SELECT * FROM dbo.OrderHeaders WHERE Id = @Id
            ", [
                 new SqlParameter("Id", orderViewModel.OrderHeader.Id),
            ]).FirstOrDefault();

            if (
                order?.PaymentStatus is not null &&
                order.PaymentStatus.Equals(SD.PaymentStatusApproved, StringComparison.CurrentCultureIgnoreCase)
            )
            {
                // Give a refund to customer
                var options = new Stripe.RefundCreateOptions
                {
                    Reason = Stripe.RefundReasons.RequestedByCustomer,
                    PaymentIntent = order.PaymentIntentId
                };

                try
                {
                    var service = new Stripe.RefundService();
                    Stripe.Refund refund = service.Create(options);
                }
                catch (Stripe.StripeException)
                {
                    // do nothing
                }

                _uow.OrderHeaders.ExecuteSql($@"
                    UPDATE dbo.OrderHeaders
                    SET
                        OrderStatus = @OrderStatus,
                        PaymentStatus = @PaymentStatus
                    WHERE (Id = @Id)
                ", [
                    new SqlParameter("Id", orderViewModel.OrderHeader.Id),
                    new SqlParameter("OrderStatus", SD.OrderStatusCancelled),
                    new SqlParameter("PaymentStatus", SD.PaymentStatusRefunded),
                ]);
            }
            else
            {
                _uow.OrderHeaders.ExecuteSql($@"
                    UPDATE dbo.OrderHeaders
                    SET
                        OrderStatus = @OrderStatus,
                        PaymentStatus = @PaymentStatus
                    WHERE (Id = @Id)
                ", [
                    new SqlParameter("Id", orderViewModel.OrderHeader.Id),
                    new SqlParameter("OrderStatus", SD.OrderStatusCancelled),
                    new SqlParameter("PaymentStatus", SD.PaymentStatusCancelled),
                ]);
            }

            TempData["success"] = "Order Cancelled Successfully";

            return RedirectToAction(nameof(Details), "Order", new { entityId = orderViewModel.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Company)]
        public IActionResult DelayedPayment(OrderViewModel orderViewModel)
        {

            if (orderViewModel.OrderHeader.Id != 0)
            {
                var orderDetails = _uow.OrderDetails.FromSql($@"
                    SELECT * FROM dbo.OrderDetails WHERE OrderHeaderId = @OrderHeaderId
                ", [new SqlParameter("OrderHeaderId", orderViewModel.OrderHeader.Id)]);

                var productIds = orderDetails.Select(od => od.ProductId).ToList();
                var inParams = new StringBuilder();
                var sqlParams = new List<SqlParameter>();
                foreach (var (Id, idx) in productIds.Select((Id, idx) => (Id, idx)))
                {
                    inParams.Append($"@p{idx},");
                    sqlParams.Add(new SqlParameter($"p{idx}", Id));
                }
                var products = _uow.Products.FromSql($@"
                    SELECT * 
                    FROM dbo.Products
                    WHERE Id IN ({inParams.ToString()[..^1]});
                ", sqlParams).ToDictionary(p => p.Id, p => p);

                foreach (var od in orderDetails)
                {
                    od.Product = products[od.ProductId];
                };

                var baseUrl = @"https://localhost:7125";

                // Stripe
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = $"{baseUrl}/Admin/Order/PaymentConfirmation?entityId={orderViewModel.OrderHeader.Id}",
                    CancelUrl = $"{baseUrl}/Admin/Order/Details?entityId={orderViewModel.OrderHeader.Id}",
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
                    Mode = "payment",
                };
                foreach (var item in orderDetails)
                {
                    options.LineItems.Add(
                        new Stripe.Checkout.SessionLineItemOptions
                        {
                            PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(item.Price * 100),
                                Currency = "usd",
                                ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = item.Product?.Title
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
                            new SqlParameter("Id", orderViewModel.OrderHeader.Id),
                            new SqlParameter("SessionId", session.Id),
                        ]);

                    Response.Headers.Append("Location", session.Url);
                    return new StatusCodeResult(303); // redirect to stipe to process payment
                }
                else
                {
                    TempData["error"] = "Oops. Something went wrong, please try again";
                    return RedirectToAction(nameof(Details), "Order", new { entityId = orderViewModel.OrderHeader.Id });
                }
            }
            else
            {
                TempData["error"] = "Oops. Something went wrong, please try again";
                return RedirectToAction(nameof(Details), "Order", new { entityId = orderViewModel.OrderHeader.Id });
                // TODO: return some error page here.
            }

            // return RedirectToAction(nameof(Details), "Order", new { entityId = orderViewModel.OrderHeader.Id });
        }

        public IActionResult PaymentConfirmation(int entityId)
        {
            var orderHeader = _uow.OrderHeaders.FromSql($@"
                SELECT * FROM dbo.OrderHeaders WHERE Id = @Id
            ", [new SqlParameter("Id", entityId)]).FirstOrDefault();

            if (orderHeader is not null)
            {
                if (orderHeader.PaymentStatus == SD.PaymentStatusApprovedDelayedPayment)
                {
                    // we need to complete processing of a company/stripe payment
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
                                PaymentDate = @PaymentDate
                            WHERE Id = @Id
                        ", [
                            new SqlParameter("Id", entityId),
                            new SqlParameter("SessionId", session.Id),
                            new SqlParameter("PaymentIntentId", session.PaymentIntentId),
                            new SqlParameter("PaymentStatus", SD.PaymentStatusApproved),
                            new SqlParameter("PaymentDate", DateTime.Now),
                        ]);

                    }
                    else
                    {
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

        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            var filterByStatus = status switch
            {
                "pending" => SD.OrderStatusPending,
                "processing" => SD.OrderStatusInProcess,
                "completed" => SD.OrderStatusShipped,
                "approved" => SD.OrderStatusApproved,
                _ => "all",
            };

            IEnumerable<OrderHeader> orderHeaders = [];
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orderHeaders = _uow.OrderHeaders.FromSql(@$"
                    SELECT * FROM dbo.OrderHeaders
                ", []);
            }
            else
            {
                var claimsIdentity = User.Identity as ClaimsIdentity;
                var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                orderHeaders = _uow.OrderHeaders.FromSql(@$"
                    SELECT * FROM dbo.OrderHeaders
                    WHERE ApplicationUserId = @ApplicationUserId
                ", [new SqlParameter("ApplicationUserId", userId)]);
            }

            var filtered = orderHeaders.Where(oh =>
                    oh.OrderStatus is null || filterByStatus.Equals("all", StringComparison.CurrentCultureIgnoreCase)
                    ? true
                        : oh.OrderStatus.Equals(filterByStatus, StringComparison.CurrentCultureIgnoreCase)
                ).ToList();

            if (filtered.Count > 0)
            {
                var applicationUserIds = filtered.Select(oh => oh.ApplicationUserId);
                var inParams = new StringBuilder();
                var sqlParams = new List<SqlParameter>();
                foreach (var (Id, idx) in applicationUserIds.Select((Id, idx) => (Id, idx)))
                {
                    inParams.Append($"@p{idx},");
                    sqlParams.Add(new SqlParameter($"p{idx}", Id));
                }
                var ApplicationUsers = _uow.ApplicationUsers.FromSql($@"
                    SELECT * FROM dbo.AspNetUsers WHERE Id in ({inParams.ToString()[..^1]})
                ", sqlParams).ToDictionary(au => au.Id, au => au);

                foreach (var orderHeader in filtered)
                {
                    orderHeader.ApplicationUser = ApplicationUsers[orderHeader.ApplicationUserId];
                }
            }

            return Json(new { data = filtered });
        }

        #endregion
    }
}