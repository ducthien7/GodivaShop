using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponController : Controller
{
    private readonly ApplicationDbContext _db;
    public CouponController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index() =>
        View(await _db.Coupons.OrderByDescending(c => c.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create(Coupon coupon)
    {
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Toggle(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon != null) { coupon.IsActive = !coupon.IsActive; await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon != null) { _db.Coupons.Remove(coupon); await _db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}