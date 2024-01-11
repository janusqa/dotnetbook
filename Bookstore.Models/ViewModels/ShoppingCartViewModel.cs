using Bookstore.Models.Domain;

namespace Bookstore.Models.ViewModels
{
    public class ShoppingCartViewModel
    {

        public required IEnumerable<ShoppingCart> ShoppingCartList { get; set; }
        public double OrderTotal { get; set; }

    }
}