using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bookstore.Models.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bookstore.Models.Domain
{
    public class ShoppingCart
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required int ProductId { get; set; }
        [ForeignKey("ProductId")]
        [ValidateNever]
        public required Product Product { get; set; }

        [Range(1, 1000, ErrorMessage = "Please enter a value between 1 and 1000")]
        public required int Count { get; set; }

        [Required]
        public string? ApplicationUserId { get; set; }
        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

    }
}