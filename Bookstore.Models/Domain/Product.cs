using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookstore.Models.Domain
{
    public class Product
    {
        [Key] // This annotation indicates to EF that this is a Primary Key
        public int Id { get; set; }

        [Required] // This annotation indicates to EF that this is a required field
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public string? ISBN { get; set; }

        [Required]
        public string? Author { get; set; }

        [Required]
        [Display(Name = "List Price")] // this is another way of putting a custom display name
        [Range(1, 1000)]
        public double ListPrice { get; set; }

        [Required]
        [Display(Name = "Price for 1-50")] // this is another way of putting a custom display name
        [Range(1, 1000)]
        public double Price { get; set; }

        [Required]
        [Display(Name = "Price for 50+")] // this is another way of putting a custom display name
        [Range(1, 1000)]
        public double Price50 { get; set; }

        [Required]
        [Display(Name = "Price for 100+")] // this is another way of putting a custom display name
        [Range(1, 1000)]
        public double Price100 { get; set; }

        // Add CateogyId as a foreign key to the Product table
        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        public string? ImageUrl { get; set; }
    }
}