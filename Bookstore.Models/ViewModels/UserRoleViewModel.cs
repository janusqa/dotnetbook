using System.ComponentModel.DataAnnotations;
using Bookstore.Models.Domain;
using Bookstore.Models.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookstore.Models.ViewModels
{
    public class UserRoleViewModel
    {
        [Required]
        public required ApplicationUserWithRole User { get; set; }

        [ValidateNever]
        public required IEnumerable<SelectListItem> Companies { get; set; }
        [ValidateNever]
        public required IEnumerable<SelectListItem> Roles { get; set; }
    }
}