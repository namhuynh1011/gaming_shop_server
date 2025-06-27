using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gaming_shop_api.Models
{
    public class Product
    {
        public int Id { get; set; }

        
        public string ProductName { get; set; }

   
        public decimal Price { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [ForeignKey("Category")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        [ForeignKey("Brand")]
        public int BrandId { get; set; }
        public Brand? Brand { get; set; }

        public bool IsHidden { get; set; }
    }
}
