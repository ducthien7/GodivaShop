using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace GodivaShop.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<LuckyPrize>LuckyPrizes { get; set; }
    public DbSet<UserSpinHistory>UserSpinHistories { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Order → UserId cho phép NULL
        builder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .IsRequired(false)          // ← QUAN TRỌNG: cho phép NULL
            .OnDelete(DeleteBehavior.SetNull);

        // Decimal precision
        builder.Entity<Order>()
            .Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Entity<Order>()
            .Property(o => o.DiscountAmount).HasPrecision(18, 2);
        builder.Entity<Product>()
            .Property(p => p.BasePrice).HasPrecision(18, 2);
        builder.Entity<ProductVariant>()
            .Property(v => v.Price).HasPrecision(18, 2);
        builder.Entity<OrderItem>()
            .Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Entity<Coupon>()
            .Property(c => c.DiscountValue).HasPrecision(18, 2);
        builder.Entity<Coupon>()
            .Property(c => c.MinOrderAmount).HasPrecision(18, 2);
    }
}