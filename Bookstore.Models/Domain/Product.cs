using System.ComponentModel.DataAnnotations;

namespace Bookstore.Models.Domain
{
    public class Product
    {
        [Key] // This annotation indicates to EF that this is a Primary Key
        public int Id { get; set; }

        [Required] // This annotation indicates to EF that this is a required field
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public required string ISBN { get; set; }

        [Required]
        public required string Author { get; set; }

        [Required]
        [Display(Name = "List Price")] // this is another way of putting a custom display name
        [Range(1, 1000)]
        public required double ListPrice { get; set; }

        [Required]
        [Display(Name = "Price for 1-50")] // this is another way of putting a custom display name
        [Range(1, 1000)]
        public required double Price { get; set; }

        [Required]
        [Display(Name = "Price for 50+")] // this is another way of putting a custom display name
        [Range(1, 1000)]
        public required double Price50 { get; set; }

        [Required]
        [Display(Name = "Price for 100+")] // this is another way of putting a custom display name
        [Range(1, 1000)]
        public required double Price100 { get; set; }
    }
}