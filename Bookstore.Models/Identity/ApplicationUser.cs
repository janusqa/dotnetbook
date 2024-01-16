using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bookstore.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Bookstore.Models.Identity
{
    // We are extending ApplicationUser. See Models for the ApplicationUsers class this is tied to
    // This is used when creating a user in the register.cshtml.cs razopage CreateUser action
    // You will need to set this up in ApplicationDbContext as well as a DbSet
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string? Name { get; set; }

        [Display(Name = "Street Address")]
        public string? StreetAddress { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        // Add CompanyId as a foreign key to the User table
        // Make it nullable as not all users belong to a company
        [Display(Name = "Company")]
        public int? CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        [ValidateNever] // this field will not be populated when creating a user so we can skip validation for now
        public Company? Company { get; set; }
    }
}