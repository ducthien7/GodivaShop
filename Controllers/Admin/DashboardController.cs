using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    // Controllers/Admin/DashboardController.cs — Cập nhật action Index
    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders.ToListAsync();
        ViewBag.TotalRevenue = orders.Sum(o => o.TotalAmount - o.DiscountAmount);
        ViewBag.TotalOrders = orders.Count;
        ViewBag.GuestOrders = orders.Count(o => o.UserId == null);
        ViewBag.MemberOrders = orders.Count(o => o.UserId != null);
        ViewBag.RecentOrders = orders.OrderByDescending(o => o.OrderDate).Take(10).ToList();

        // Đếm theo trạng thái cho biểu đồ
        ViewBag.StatusCounts = new int[] {
        orders.Count(o => o.Status == OrderStatus.Pending),
        orders.Count(o => o.Status == OrderStatus.Confirmed),
        orders.Count(o => o.Status == OrderStatus.Processing),
        orders.Count(o => o.Status == OrderStatus.Shipped),
        orders.Count(o => o.Status == OrderStatus.Delivered),
        orders.Count(o => o.Status == OrderStatus.Cancelled),
    };
        return View();
    }
}