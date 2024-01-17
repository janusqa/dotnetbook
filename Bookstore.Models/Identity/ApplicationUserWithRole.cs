using System.ComponentModel.DataAnnotations.Schema;
using Bookstore.Models.Domain;

namespace Bookstore.Models.Identity
{
    public class ApplicationUserWithRole : ApplicationUser
    {
        public string? RoleId { get; set; }
        public string? RoleName { get; set; }
        [NotMapped]
        public new Company? Company { get; set; }
    }
}