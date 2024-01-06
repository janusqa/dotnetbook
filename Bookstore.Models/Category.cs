using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Models
{
    public class Category
    {
        [Key] // This annotation indicates to EF that this is a Primary Key
        public int Id { get; set; }

        [Required] // This annotation indicates to EF that this is a required field
        [MaxLength(30)] // specify the maxlen a Name can be
        public required string Name { get; set; }

        [DisplayName("Display Order")] // This annotation indicates to EF that this is the text we want to display for this field in the UI when using asp-for
        [Range(1, 100)] // specfiy the range of values DisplayOrder can take
        // We can have custome error message attached with annotations too. Below is an example.
        // [Range(1, 100, ErrorMessage ="The field Display Order must be between 1 and 100")]
        public int DisplayOrder { get; set; }
    }
}