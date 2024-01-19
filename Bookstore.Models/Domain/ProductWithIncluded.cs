using System.ComponentModel.DataAnnotations.Schema;

namespace Bookstore.Models.Domain
{
    public class ProductWithIncluded : Product
    {
        public required string CategoryName { get; set; }

        public int CategoryDisplayOrder { get; set; }

        [NotMapped]
        public new Category? Category { get; set; }

        public int? ProductImageId { get; set; }

        public string? ProductImageUrl { get; set; }
    }
}