using GodivaShop.Web.Data;
using GodivaShop.Web.Hubs;
using GodivaShop.Web.Models.Domain;
using GodivaShop.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// === Database ===
/*
// Cách này sẽ lưu DB ở thư mục gốc của Project thay vì trong bin/Debug
// 1. Lấy đường dẫn thư mục gốc của dự án (nơi chứa file .csproj)
string projectRoot = Directory.GetCurrentDirectory();

// 2. Tạo một thư mục tên là "Data" bên trong dự án (nếu chưa có) để chứa DB cho gọn
string dbFolder = Path.Combine(projectRoot, "Data");
if (!Directory.Exists(dbFolder))
{
    Directory.CreateDirectory(dbFolder);
}

// 3. Gán đường dẫn này cho |DataDirectory|
AppDomain.CurrentDomain.SetData("DataDirectory", dbFolder);
*/
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

// === Identity ===
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// === Google OAuth ===
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });

// === Session (Giỏ hàng) ===
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// === Services ===
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<BestSellerService>();

// === SignalR ===
builder.Services.AddSignalR();

builder.Services.AddControllersWithViews();

builder.Services.AddRazorPages();

var app = builder.Build();

// === Middleware ===
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();           // ← PHẢI trước UseAuthentication
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
// === Routes ===
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<OrderHub>("/orderHub");

// === Seed Data ===
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();