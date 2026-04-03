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
    public async Task<IActionResult> Category(string? slug, decimal? minPrice, decimal? maxPrice, string? search)
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

    // Trang chi tiết sản phẩm
    public async Task<IActionResult> Detail(int id)
    {
        ViewBag.CartCount = _cart.GetCartCount();

        var product = await _db.Products
            .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
            .Include(p => p.Variants
                .Where(v => v.IsActive)
                .OrderBy(v => v.Price))
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
    public async Task<IActionResult> AddToCart([FromBody] CartItem item)
    {
        int stockQuantity = 0;

        // 1. NẾU KHÁCH CHỌN BIẾN THỂ (SIZE / SỐ LƯỢNG VIÊN)
        if (item.VariantId.HasValue && item.VariantId > 0)
        {
            var variant = await _db.ProductVariants.FindAsync(item.VariantId);
            if (variant == null) return Json(new { success = false, message = "Biến thể không tồn tại." });

            // Lấy tồn kho chuẩn từ bảng ProductVariant
            stockQuantity = variant.StockQuantity;
        }
        else
        {
            // 2. NẾU KHÁCH MUA SẢN PHẨM GỐC (CHƯA CHỌN BIẾN THỂ)
            var product = await _db.Products
                                   .Include(p => p.Variants)
                                   .FirstOrDefaultAsync(p => p.Id == item.ProductId);

            if (product == null) return Json(new { success = false, message = "Sản phẩm không tồn tại." });

            // Nếu sản phẩm đó CÓ CÀI ĐẶT biến thể mà khách chưa chọn -> Ép khách phải chọn
            if (product.Variants != null && product.Variants.Any(v => v.IsActive))
            {
                return Json(new { success = false, message = "Vui lòng chọn phân loại sản phẩm (kích thước/số lượng)!" });
            }

            // Nếu sản phẩm ĐƠN GIẢN (không có biến thể), vì bảng Product của bạn chưa có cột tồn kho
            // -> Mình tạm set 999 để cho qua. (Sau này nếu muốn, bạn thêm public int StockQuantity vào Product.cs sau nhé).
            stockQuantity = 999;
        }

        // 3. KIỂM TRA SỐ LƯỢNG ĐÃ CÓ TRONG GIỎ
        var cartItems = _cart.GetCart();
        var existingItem = cartItems.FirstOrDefault(c => c.ProductId == item.ProductId && c.VariantId == item.VariantId);

        int currentInCart = existingItem != null ? existingItem.Quantity : 0;
        int totalRequested = currentInCart + item.Quantity;

        // 4. CHẶN ĐỨNG NẾU LỐ TỒN KHO
        if (totalRequested > stockQuantity)
        {
            if (currentInCart > 0)
            {
                return Json(new
                {
                    success = false,
                    message = $"Kho chỉ còn {stockQuantity} hộp. Bạn đã có sẵn {currentInCart} hộp trong giỏ rồi nhé!"
                });
            }
            return Json(new
            {
                success = false,
                message = $"Rất tiếc! Số lượng tồn kho hiện tại chỉ còn {stockQuantity} hộp."
            });
        }

        // 5. VƯỢT QUA CỬA ẢI THÌ CHO VÀO GIỎ
        _cart.AddItem(item);
        return Json(new { success = true, cartCount = _cart.GetCartCount() });
    }

    // ==========================================
    // LOGIC BÁN CHẠY NHẤT (ĐÃ NÂNG CẤP)
    // ==========================================
    public async Task<IActionResult> BestSellers()
    {
        ViewBag.CartCount = _cart.GetCartCount();
        ViewBag.Categories = await _db.Categories
            .Where(c => c.IsActive).ToListAsync();

        // 1. TỰ ĐỘNG: Lấy ID của top 12 sản phẩm bán chạy nhất từ Service
        var autoTopSellerIds = (await _bestSeller.GetTopSellingProductsAsync(12))
            .Select(x => x.ProductId)
            .ToList();

        // 2. KẾT HỢP: Lấy sản phẩm có IsBestSeller (Admin ghim) HOẶC nằm trong Top tự động
        var bestSellers = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Where(p => p.IsActive && (p.IsBestSeller || autoTopSellerIds.Contains(p.Id)))
            .ToListAsync();

        // 3. SẮP XẾP THÔNG MINH: Ưu tiên hàng Admin ghim lên trước, sau đó mới xếp theo thứ tự bán chạy
        var orderedBestSellers = bestSellers
            .OrderByDescending(p => p.IsBestSeller)
            .ThenBy(p => autoTopSellerIds.IndexOf(p.Id) == -1 ? int.MaxValue : autoTopSellerIds.IndexOf(p.Id))
            .ToList();

        ViewBag.PageTitle = "Sản Phẩm Bán Chạy Nhất";
        return View("Category", orderedBestSellers);
    }
}