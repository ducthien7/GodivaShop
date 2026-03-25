// Controllers/Admin/CategoryController.cs
using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
	private readonly ApplicationDbContext _db;
	public CategoryController(ApplicationDbContext db) => _db = db;

	public async Task<IActionResult> Index() =>
		View(await _db.Categories.Include(c => c.Products).ToListAsync());

	[HttpPost]
	public async Task<IActionResult> Create(string name, string? slug, int displayOrder)
	{
		_db.Categories.Add(new Category
		{
			Name = name,
			Slug = slug ?? name.ToLower().Replace(" ", "-"),
			DisplayOrder = displayOrder,
			IsActive = true
		});
		await _db.SaveChangesAsync();
		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	public async Task<IActionResult> Toggle(int id)
	{
		var cat = await _db.Categories.FindAsync(id);
		if (cat != null) { cat.IsActive = !cat.IsActive; await _db.SaveChangesAsync(); }
		return RedirectToAction(nameof(Index));
	}
}