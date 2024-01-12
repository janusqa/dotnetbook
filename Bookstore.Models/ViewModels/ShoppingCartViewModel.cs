using Bookstore.Models.Domain;

namespace Bookstore.Models.ViewModels
{
    public class ShoppingCartViewModel
    {

        public IEnumerable<ShoppingCart> ShoppingCartList { get; set; }
        public OrderHeader OrderHeader { get; set; }

        // Refactor, since adding OrderHeader which contains OrderTotal, we can remove this property
        //public double OrderTotal { get; set; } 

        public ShoppingCartViewModel(IEnumerable<ShoppingCart> shoppingCartList)
        {
            ShoppingCartList = shoppingCartList;
            OrderHeader = new OrderHeader();
        }
    }
}