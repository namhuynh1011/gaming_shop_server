using System.ComponentModel.DataAnnotations;

namespace gaming_shop_api.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string BrandName { get; set; }

    }
}
