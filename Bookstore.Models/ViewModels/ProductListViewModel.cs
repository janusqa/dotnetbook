using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Bookstore.Models.Domain;

namespace Bookstore.Models.ViewModels
{
    public class ProductListViewModel : Product
    {
        // public required int Id { get; set; }

        // public required string Title { get; set; }

        // public required string Description { get; set; }

        // public required string ISBN { get; set; }

        // public required string Author { get; set; }

        // public required double ListPrice { get; set; }

        // public required double Price { get; set; }

        // public required double Price50 { get; set; }

        // public required double Price100 { get; set; }

        // public required string CategoryId { get; set; }

        public new required string Category { get; set; }

        // public required string ImageUrl { get; set; }
    }
}