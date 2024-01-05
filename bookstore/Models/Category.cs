using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace bookstore.Models
{
    public class Category
    {
        [Key] // This annotation indicates to EF that this is a Primary Key
        public int Id { get; set; }

        [Required] // This annotation indicates to EF that this is a required field
        public required string Name { get; set; }

        [DisplayName("Display Order")] // This annotation indicates to EF that this is the text we want to display for this field in the UI when using asp-for
        public int DisplayOrder { get; set; }
    }
}