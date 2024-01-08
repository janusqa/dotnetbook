using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bookstore.Models.ViewModels
{
    public class ProductListViewModel
    {
        public required int Id { get; set; }

        public required string Title { get; set; }

        public required string ISBN { get; set; }

        public required string Author { get; set; }

        public required double Price { get; set; }

        public required string Category { get; set; }
    }
}