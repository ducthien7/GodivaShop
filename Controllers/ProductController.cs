using GodivaShop.Web.Data;
using GodivaShop.Web.Models.ViewModels;
using GodivaShop.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers;

public class ProductController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly CartService _cart;
    private readonly BestSellerService _bestSeller;

    public ProductController(ApplicationDbContext db, CartService cart, BestSellerService bestSeller)
    {
        _db = db;
        _cart = cart;
        _bestSeller = bestSeller;
    }

    // Trang danh mục
    public async Task<IActionResult> Category(
   string? slug, decimal? minPrice, decimal? maxPrice, string? search)
    {
        ViewBag.CartCount = _cart.GetCartCount();
        ViewBag.Categories = await _db.Categories
            .Where(c => c.IsActive).ToListAsync();

        var query = _db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(slug))
            query = query.Where(p => p.Category.Slug == slug);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(p =>
                p.Name.Contains(search) ||
                (p.Description != null && p.Description.Contains(search)));

        if (minPrice.HasValue)
            query = query.Where(p => p.BasePrice >= minPrice);

        if (maxPrice.HasValue)
            query = query.Where(p => p.BasePrice <= maxPrice);

        ViewBag.CurrentSlug = slug;
        ViewBag.SearchTerm = search;

        return View(await query.ToListAsync());
    }
    /*
    public async Task<IActionResult> Category(string? slug, decimal? minPrice, decimal? maxPrice)
    {
        ViewBag.CartCount = _cart.GetCartCount();
        var categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
        ViewBag.Categories = categories;

        var query = _db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (!string.IsNullOrEmpty(slug))
            query = query.Where(p => p.Category.Slug == slug);
        if (minPrice.HasValue)
            query = query.Where(p => p.BasePrice >= minPrice);
        if (maxPrice.HasValue)
            query = query.Where(p => p.BasePrice <= maxPrice);

        var products = await query.ToListAsync();
        ViewBag.CurrentSlug = slug;
        return View(products);
    }
    */
    // Trang chi tiết sản phẩm
    public async Task<IActionResult> Detail(int id)
    {
        ViewBag.CartCount = _cart.GetCartCount();
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (product == null) return NotFound();
        return View(product);
    }

    // AJAX: Lấy giá biến thể
    [HttpGet]
    public async Task<IActionResult> GetVariantPrice(int variantId)
    {
        var variant = await _db.ProductVariants.FindAsync(variantId);
        if (variant == null) return NotFound();
        return Json(new { price = variant.Price, name = variant.Name });
    }

    // AJAX: Thêm vào giỏ
    [HttpPost]
    public IActionResult AddToCart([FromBody] CartItem item)
    {
        _cart.AddItem(item);
        return Json(new { success = true, cartCount = _cart.GetCartCount() });
    }

    // Trang bán chạy nhất
    public async Task<IActionResult> BestSellers()
    {
        ViewBag.CartCount = _cart.GetCartCount();
        ViewBag.Categories = await _db.Categories
            .Where(c => c.IsActive).ToListAsync();

        // Lấy danh sách ID của sản phẩm bán chạy nhất
        var topSellerIds = (await _bestSeller.GetTopSellingProductsAsync(12))
            .Select(x => x.ProductId)
            .ToList();

        // Lấy chi tiết sản phẩm
        var bestSellers = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive && topSellerIds.Contains(p.Id))
            .ToListAsync();

        // Sắp xếp theo thứ tự top sellers
        var orderedBestSellers = bestSellers
            .OrderBy(p => topSellerIds.IndexOf(p.Id))
            .ToList();

        ViewBag.PageTitle = "Sản Phẩm Bán Chạy Nhất";
        return View("Category", orderedBestSellers);
    }

}