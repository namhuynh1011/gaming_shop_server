using System.ComponentModel.DataAnnotations;

namespace gaming_shop_api.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string CategoryName { get; set; }

    }
}
