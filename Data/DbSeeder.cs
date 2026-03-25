using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        // === Seed Roles ===
        string[] roles = { "Admin", "Staff", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // === Seed Admin Account ===
        const string adminEmail = "admin@godiva.com";
        const string adminPassword = "Admin@123456";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Super Admin",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }

        // === Seed Categories ===
        if (!await context.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new() { Name = "Assorted Chocolates", Slug = "assorted", DisplayOrder = 1 },
                new() { Name = "Dark Chocolate", Slug = "dark-chocolate", DisplayOrder = 2 },
                new() { Name = "Milk Chocolate", Slug = "milk-chocolate", DisplayOrder = 3 },
                new() { Name = "Kosher Chocolate", Slug = "kosher", DisplayOrder = 4 },
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        // === Seed Coupon ===
        if (!await context.Coupons.AnyAsync())
        {
            context.Coupons.Add(new Coupon
            {
                Code = "WELCOME10",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                MinOrderAmount = 200000,
                IsActive = true
            });
            await context.SaveChangesAsync();
        }
    }
}