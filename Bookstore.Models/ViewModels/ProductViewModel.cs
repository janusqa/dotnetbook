using System.ComponentModel.DataAnnotations;
using Bookstore.Models.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookstore.Models.ViewModels
{
    public class ProductViewModel
    {
        [Required]
        public required Product Product { get; set; }

        // System.NullReferenceException: Object reference not set to an instance of an object.
        // ValidateNever fixes this
        [ValidateNever]
        public IEnumerable<SelectListItem>? CategoryList { get; set; }
    }
}