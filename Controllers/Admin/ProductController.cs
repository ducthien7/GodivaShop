using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using GodivaShop.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    // ===== INDEX =====
    public async Task<IActionResult> Index()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(products);
    }

    // ===== CREATE GET =====
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
        return View(new ProductCreateViewModel { IsActive = true });
    }

    // ===== CREATE POST =====
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel vm)
    {
        // Debug: In ra lỗi ModelState nếu có
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _db.Categories
                .Where(c => c.IsActive).ToListAsync();

            // Hiển thị lỗi cụ thể để debug
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            TempData["Error"] = string.Join(" | ", errors);
            return View(vm);
        }

        // Tạo Product từ ViewModel
        var product = new Product
        {
            Name = vm.Name.Trim(),
            Description = vm.Description,
            BasePrice = vm.BasePrice,
            CategoryId = vm.CategoryId,
            IsBestSeller = vm.IsBestSeller,
            IsActive = vm.IsActive,
            Slug = GenerateSlug(vm.Name),
            CreatedAt = DateTime.Now
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(); // Lưu để có product.Id

        // Upload ảnh
        if (vm.Images != null && vm.Images.Any())
        {
            bool isFirst = true;
            foreach (var file in vm.Images)
            {
                if (file.Length > 0)
                {
                    var savedPath = await SaveImageAsync(file);
                    _db.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        ImagePath = savedPath,
                        IsMain = isFirst,
                        DisplayOrder = isFirst ? 0 : 1
                    });
                    isFirst = false;
                }
            }
            await _db.SaveChangesAsync();
        }

        TempData["Success"] = $"Đã thêm sản phẩm '{product.Name}' thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ===== EDIT GET =====
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        ViewBag.Categories = await _db.Categories
            .Where(c => c.IsActive).ToListAsync();

        var vm = new ProductCreateViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            CategoryId = product.CategoryId,
            IsBestSeller = product.IsBestSeller,
            IsActive = product.IsActive
        };

        ViewBag.ExistingImages = product.Images;
        ViewBag.Variants = product.Variants;
        return View(vm);
    }

    // ===== EDIT POST =====
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _db.Categories
                .Where(c => c.IsActive).ToListAsync();
            var product2 = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == vm.Id);
            ViewBag.ExistingImages = product2?.Images;
            ViewBag.Variants = product2?.Variants;
            return View(vm);
        }

        var product = await _db.Products.FindAsync(vm.Id);
        if (product == null) return NotFound();

        product.Name = vm.Name.Trim();
        product.Description = vm.Description;
        product.BasePrice = vm.BasePrice;
        product.CategoryId = vm.CategoryId;
        product.IsBestSeller = vm.IsBestSeller;
        product.IsActive = vm.IsActive;
        product.Slug = GenerateSlug(vm.Name);

        // Upload ảnh mới nếu có
        if (vm.Images != null && vm.Images.Any(f => f.Length > 0))
        {
            foreach (var file in vm.Images.Where(f => f.Length > 0))
            {
                var savedPath = await SaveImageAsync(file);
                _db.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImagePath = savedPath,
                    IsMain = false
                });
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật sản phẩm thành công!";
        return RedirectToAction(nameof(Index));
    }

    // ===== DELETE (Soft delete) =====
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product != null)
        {
            product.IsActive = false;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã ẩn sản phẩm thành công!";
        }
        return RedirectToAction(nameof(Index));
    }

    // ===== AJAX: Add Variant =====
    [HttpPost]
    public async Task<IActionResult> AddVariant(
        int productId, string name, decimal price, int stockQuantity)
    {
        if (string.IsNullOrEmpty(name) || price <= 0)
            return Json(new { success = false, message = "Dữ liệu không hợp lệ" });

        var variant = new ProductVariant
        {
            ProductId = productId,
            Name = name,
            Price = price,
            StockQuantity = stockQuantity,
            IsActive = true
        };
        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync();

        return Json(new { success = true, id = variant.Id, name, price, stockQuantity });
    }

    // ===== AJAX: Delete Variant =====
    [HttpPost]
    public async Task<IActionResult> DeleteVariant(int id)
    {
        var variant = await _db.ProductVariants.FindAsync(id);
        if (variant != null)
        {
            _db.ProductVariants.Remove(variant);
            await _db.SaveChangesAsync();
        }
        return Json(new { success = true });
    }

    // ===== AJAX: Delete Image =====
    [HttpPost]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var img = await _db.ProductImages.FindAsync(id);
        if (img != null)
        {
            // Xóa file vật lý
            var relativePath = img.ImagePath.Replace("~/", "");
            var fullPath = Path.Combine(_env.WebRootPath,
                relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);

            _db.ProductImages.Remove(img);
            await _db.SaveChangesAsync();
        }
        return Json(new { success = true });
    }

    // ===== AJAX: Set Main Image =====
    [HttpPost]
    public async Task<IActionResult> SetMainImage(int imageId, int productId)
    {
        var images = await _db.ProductImages
            .Where(i => i.ProductId == productId).ToListAsync();
        foreach (var img in images)
            img.IsMain = (img.Id == imageId);
        await _db.SaveChangesAsync();
        return Json(new { success = true });
    }

    // ===== HELPER: Lưu ảnh =====
    private async Task<string> SaveImageAsync(IFormFile file)
    {
        // Đảm bảo thư mục tồn tại
        var uploadDir = Path.Combine(_env.WebRootPath, "images", "products");
        if (!Directory.Exists(uploadDir))
            Directory.CreateDirectory(uploadDir);

        var ext = Path.GetExtension(file.FileName).ToLower();
        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(uploadDir, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"~/images/products/{fileName}";
    }

    // ===== HELPER: Tạo slug =====
    private static string GenerateSlug(string name)
    {
        return name.ToLower()
            .Replace(" ", "-")
            .Replace("đ", "d")
            .Replace("à", "a").Replace("á", "a").Replace("ả", "a")
            .Replace("ã", "a").Replace("ạ", "a").Replace("ă", "a")
            .Replace("ằ", "a").Replace("ắ", "a").Replace("ẵ", "a")
            .Replace("ặ", "a").Replace("â", "a").Replace("ầ", "a")
            .Replace("ấ", "a").Replace("ẩ", "a").Replace("ẫ", "a")
            .Replace("ậ", "a").Replace("è", "e").Replace("é", "e")
            .Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
            .Replace("ê", "e").Replace("ề", "e").Replace("ế", "e")
            .Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
            .Replace("ì", "i").Replace("í", "i").Replace("ỉ", "i")
            .Replace("ĩ", "i").Replace("ị", "i").Replace("ò", "o")
            .Replace("ó", "o").Replace("ỏ", "o").Replace("õ", "o")
            .Replace("ọ", "o").Replace("ô", "o").Replace("ồ", "o")
            .Replace("ố", "o").Replace("ổ", "o").Replace("ỗ", "o")
            .Replace("ộ", "o").Replace("ơ", "o").Replace("ờ", "o")
            .Replace("ớ", "o").Replace("ở", "o").Replace("ỡ", "o")
            .Replace("ợ", "o").Replace("ù", "u").Replace("ú", "u")
            .Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
            .Replace("ư", "u").Replace("ừ", "u").Replace("ứ", "u")
            .Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
            .Replace("ỳ", "y").Replace("ý", "y").Replace("ỷ", "y")
            .Replace("ỹ", "y").Replace("ỵ", "y");
    }

    // Toggle Best Seller status
    [HttpPost]
    public async Task<IActionResult> ToggleBestSeller(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return Json(new { success = false, message = "Sản phẩm không tồn tại" });

        product.IsBestSeller = !product.IsBestSeller;
        _db.Products.Update(product);
        await _db.SaveChangesAsync();

        return Json(new { success = true, isBestSeller = product.IsBestSeller });
    }
}