using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Bookstore.Models.Identity
{
    // We are extending IdentityUser. See Models for the ApplicationUsers class this is tied to
    // This is used when creating a user in the register.cshtml.cs razopage CreateUser action
    // You will need to set this up in ApplicationDbContext as well as a DbSet
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string? Name { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
    }
}