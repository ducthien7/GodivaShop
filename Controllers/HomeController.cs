// Controllers/HomeController.cs
using GodivaShop.Web.Data;
using GodivaShop.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly CartService _cart;

    public HomeController(ApplicationDbContext db, CartService cart)
    {
        _db = db;
        _cart = cart;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.CartCount = _cart.GetCartCount();

        var featured = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.IsBestSeller)
            .ThenByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync();

        return View(featured);
    }
}