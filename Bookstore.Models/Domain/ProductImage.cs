using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bookstore.Models.Domain
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public required string ImageUrl { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public Product? Product { get; set; }
    }
}