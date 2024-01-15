using Bookstore.Models.Domain;

namespace Bookstore.Models.ViewModels
{
    public class OrderViewModel
    {
        public required OrderHeader OrderHeader { get; set; }
        public required IEnumerable<OrderDetail> OrderDetails { get; set; }
    }
}