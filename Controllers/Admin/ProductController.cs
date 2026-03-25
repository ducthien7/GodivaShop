using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<IActionResult> Index() =>
        View(await _db.Products.Include(p => p.Category).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile file, int productId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file");

        // Tên file duy nhất bằng GUID
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var savePath = Path.Combine(_env.WebRootPath, "images", "products", fileName);

        using var stream = new FileStream(savePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var image = new ProductImage
        {
            ProductId = productId,
            ImagePath = $"~/images/products/{fileName}"
        };
        _db.ProductImages.Add(image);
        await _db.SaveChangesAsync();

        return Json(new { success = true, path = image.ImagePath });
    }

    // Thêm vào ProductController (Admin)
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product model, List<IFormFile> images)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(model);
        }

        model.Slug = model.Name.ToLower().Replace(" ", "-");
        _db.Products.Add(model);
        await _db.SaveChangesAsync();

        // Upload ảnh
        bool isFirst = true;
        foreach (var file in images)
        {
            if (file.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var savePath = Path.Combine(_env.WebRootPath, "images", "products", fileName);
                using var stream = new FileStream(savePath, FileMode.Create);
                await file.CopyToAsync(stream);
                _db.ProductImages.Add(new ProductImage
                {
                    ProductId = model.Id,
                    ImagePath = $"~/images/products/{fileName}",
                    IsMain = isFirst,
                    DisplayOrder = isFirst ? 0 : 1
                });
                isFirst = false;
            }
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();
        ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Product model, List<IFormFile> newImages)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _db.Categories.ToListAsync();
            return View(model);
        }

        var existing = await _db.Products.FindAsync(model.Id);
        if (existing == null) return NotFound();

        existing.Name = model.Name;
        existing.Description = model.Description;
        existing.BasePrice = model.BasePrice;
        existing.CategoryId = model.CategoryId;
        existing.IsBestSeller = model.IsBestSeller;
        existing.IsActive = model.IsActive;
        existing.Slug = model.Name.ToLower().Replace(" ", "-");

        foreach (var file in newImages)
        {
            if (file.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var savePath = Path.Combine(_env.WebRootPath, "images", "products", fileName);
                using var stream = new FileStream(savePath, FileMode.Create);
                await file.CopyToAsync(stream);
                _db.ProductImages.Add(new ProductImage
                {
                    ProductId = model.Id,
                    ImagePath = $"~/images/products/{fileName}"
                });
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product != null) { product.IsActive = false; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    // AJAX: Thêm variant
    [HttpPost]
    public async Task<IActionResult> AddVariant(ProductVariant variant)
    {
        _db.ProductVariants.Add(variant);
        await _db.SaveChangesAsync();
        return Json(new { success = true, id = variant.Id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteVariant(int id)
    {
        var v = await _db.ProductVariants.FindAsync(id);
        if (v != null) { _db.ProductVariants.Remove(v); await _db.SaveChangesAsync(); }
        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var img = await _db.ProductImages.FindAsync(id);
        if (img != null)
        {
            var path = Path.Combine(_env.WebRootPath, img.ImagePath.Replace("~/", "").Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            _db.ProductImages.Remove(img);
            await _db.SaveChangesAsync();
        }
        return Json(new { success = true });
    }
}