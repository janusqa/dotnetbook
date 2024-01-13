using Bookstore.Models.Domain;

namespace Bookstore.Models.ViewModels
{
    public class ShoppingCartViewModel
    {

        public required IEnumerable<ShoppingCart> ShoppingCartList { get; set; }
        public required OrderHeader OrderHeader { get; set; }

        // Refactor, since adding OrderHeader which contains OrderTotal, we can remove this property
        //public double OrderTotal { get; set; } 
    }
}