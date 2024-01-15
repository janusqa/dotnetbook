// View component code behind file names MUST end with "ViewComponent"
// The code behind files are placed in a ViewComponents folder in root of project
// View component view files on the other hand must be placed in a sub directiory
// in the Views/Shared folder called Components. Then within this folder would 
// have the folders for each of our components. This one for example is a 
// shoppingcart component to keep track of number of items in cart, so 
// we can eaisly display it in various places on the website. The folder name
// must match back with the code behind file. So for example if the 
// code behind file is ShoppingCartViewModel, then the chstml file will be
// Views/Shared/Components/ShoppingCart/Default.cshtml.
// In this file put the model the page will use and other business logic you need.
// Now to use it go to the cshtml page where you want to place it 
// and insert in the html some razor code like "@await Component.InvokeAsync("ShoppingCart")"

using System.Security.Claims;
using Bookstore.Utility;
using BookStore.DataAccess.UnitOfWork.IUnitOfWork;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace bookstore.ViewComponents
{
    public class ShoppingCartViewComponent : ViewComponent
    {

        private readonly IUnitOfWork _uow;

        public ShoppingCartViewComponent(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // we need to store the number of distince user items a user has in a session so we can display it in 
            // header of site.  We must make a database call to get the number of items in cart for this logged in user
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId is not null)
            {
                if (HttpContext.Session.GetInt32(SD.SessionCart) is null)
                {
                    var itemCount = _uow.ShoppingCarts.SqlQuery<int>($@"
                        SELECT COUNT(Id) FROM dbo.ShoppingCarts WHERE ApplicationUserId = @ApplicationUserId
                    ", [new SqlParameter("ApplicationUserId", userId)])?.FirstOrDefault();
                    HttpContext.Session.SetInt32(SD.SessionCart, itemCount ?? 0);
                }
                return View(HttpContext.Session.GetInt32(SD.SessionCart));
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}