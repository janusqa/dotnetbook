using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bookstore.Models.Domain
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderHeaderId { get; set; }
        [ForeignKey("OrderHeaderId")]
        [ValidateNever]
        public OrderHeader? OrderHeader { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product? Product { get; set; }

        public int Count { get; set; }

        public double Price { get; set; }
    }
}