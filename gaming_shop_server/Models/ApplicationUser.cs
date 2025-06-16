using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace gaming_shop_api.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; }
        public string? Address { get; set; }
    }
}
