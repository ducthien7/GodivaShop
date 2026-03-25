using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;

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

        // (No image assignment logic) — keep seeder minimal

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

        // === Seed Products (ensure 4 products per category, add missing ones only) ===
        {
            var categoriesList = await context.Categories.ToListAsync();
            var sampleImages = new[] { "~/images/products/sample1.svg", "~/images/products/sample2.svg", "~/images/products/sample3.svg" };
            int imgIndex = 0;

            foreach (var cat in categoriesList)
            {
                var existingCount = await context.Products.CountAsync(p => p.CategoryId == cat.Id);
                if (existingCount >= 4) continue;

                for (int idx = existingCount; idx < 4; idx++)
                {
                    var seq = idx + 1;
                    var price = 150000 + (seq * 50000) + (cat.Id * 1000);
                    var prod = new Product
                    {
                        Name = $"{cat.Name} Box {seq}",
                        Description = $"{cat.Name} - Hộp quà số {seq} với các hương vị tuyển chọn.",
                        Slug = $"{cat.Slug}-box-{seq}-{Guid.NewGuid().ToString().Substring(0, 6)}",
                        BasePrice = price,
                        IsBestSeller = (seq == 1 && existingCount == 0),
                        IsActive = true,
                        CategoryId = cat.Id
                    };

                    context.Products.Add(prod);
                    await context.SaveChangesAsync(); // need Id for variant/image

                    // Add one default variant
                    context.ProductVariants.Add(new ProductVariant
                    {
                        ProductId = prod.Id,
                        Name = "Hộp",
                        Price = prod.BasePrice,
                        StockQuantity = 50,
                        IsActive = true
                    });

                    // Add one image (rotate sample images)
                    context.ProductImages.Add(new ProductImage
                    {
                        ProductId = prod.Id,
                        ImagePath = sampleImages[imgIndex % sampleImages.Length],
                        IsMain = true,
                        DisplayOrder = 0
                    });

                    imgIndex++;
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}