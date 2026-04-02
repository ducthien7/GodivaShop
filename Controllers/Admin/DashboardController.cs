using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders.ToListAsync();
        var products = await _db.Products.ToListAsync();

        ViewBag.TotalOrders = orders.Count;
        ViewBag.TotalRevenue = orders.Sum(o => o.TotalAmount - o.DiscountAmount);
        ViewBag.GuestOrders = orders.Count(o => o.UserId == null);
        ViewBag.MemberOrders = orders.Count(o => o.UserId != null);
        ViewBag.TotalProducts = products.Count(p => p.IsActive);
        ViewBag.RecentOrders = orders
            .OrderByDescending(o => o.OrderDate)
            .Take(8)
            .ToList();

        // Doanh thu 6 tháng gần nhất
        var sixMonths = Enumerable.Range(0, 6)
            .Select(i => DateTime.Now.AddMonths(-i))
            .Reverse()
            .ToList();

        ViewBag.MonthLabels = sixMonths
            .Select(m => $"T{m.Month}/{m.Year % 100}")
            .ToList();

        ViewBag.MonthRevenue = sixMonths
            .Select(m => orders
                .Where(o => o.OrderDate.Month == m.Month
                         && o.OrderDate.Year == m.Year)
                .Sum(o => o.TotalAmount - o.DiscountAmount))
            .ToList();

        ViewBag.MonthOrders = sixMonths
            .Select(m => orders
                .Count(o => o.OrderDate.Month == m.Month
                          && o.OrderDate.Year == m.Year))
            .ToList();

        // Đếm theo trạng thái
        ViewBag.StatusCounts = new int[]
        {
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