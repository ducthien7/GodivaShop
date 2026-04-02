using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using GodivaShop.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers.Admin;

[Area("Admin")]
[Authorize(Roles = "Admin,Staff")]
public class OrderController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly EmailService _emailService;

    public OrderController(ApplicationDbContext db, EmailService emailService)
    {
        _db = db;
        _emailService = emailService;
    }

    public async Task<IActionResult> Index()
    {
        var orders = await _db.Orders
            .Include(o => o.OrderItems)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
        return View(orders);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        await _db.SaveChangesAsync();

        // Gửi email cập nhật
        await _emailService.SendOrderStatusUpdateAsync(
            order.GuestEmail, order.GuestFullName, order.Id, status.ToString());

        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Detail(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.Images)
            .Include(o => o.OrderItems)
                .ThenInclude(i => i.ProductVariant)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();
        return View(order);
    }
    // Xóa đơn hàng
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        _db.OrderItems.RemoveRange(order.OrderItems);
        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa đơn hàng #{id} thành công";
        return RedirectToAction(nameof(Index));
    }

    // Cập nhật trạng thái (sửa lại để nhận int thay vì enum trực tiếp)
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, int status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = (OrderStatus)status;
        await _db.SaveChangesAsync();

        // Gửi email thông báo
        var statusText = order.Status switch
        {
            OrderStatus.Pending => "Chờ xử lý",
            OrderStatus.Confirmed => "Đã xác nhận",
            OrderStatus.Processing => "Đang xử lý",
            OrderStatus.Shipped => "Đang giao hàng",
            OrderStatus.Delivered => "Đã giao",
            OrderStatus.Cancelled => "Đã hủy",
            _ => order.Status.ToString()
        };
        await _emailService.SendOrderStatusUpdateAsync(
            order.GuestEmail, order.GuestFullName, order.Id, statusText);

        TempData["Success"] = $"Đã cập nhật trạng thái đơn #{id} thành '{statusText}'";
        return RedirectToAction(nameof(Index));
    }
}