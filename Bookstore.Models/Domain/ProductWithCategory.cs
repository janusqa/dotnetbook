using System.ComponentModel.DataAnnotations.Schema;

namespace Bookstore.Models.Domain
{
    public class ProductWithCategory : Product
    {
        public required string CategoryName { get; set; }
        public int CategoryDisplayOrder { get; set; }

        [NotMapped]
        public new Category? Category { get; set; }
    }
}