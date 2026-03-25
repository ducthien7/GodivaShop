using Microsoft.AspNetCore.Identity;

namespace GodivaShop.Web.Models.Domain;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}