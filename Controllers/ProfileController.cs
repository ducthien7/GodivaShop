using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // <--- THÊM MỚI

        // Cập nhật Constructor để nhận thêm SignInManager
        public ProfileController(ApplicationDbContext db,
                                 UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager) // <--- THÊM MỚI
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager; // <--- THÊM MỚI
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Không tìm thấy thông tin người dùng.");

            var orderHistory = await _db.Orders
                                        .Where(o => o.UserId == user.Id)
                                        .OrderByDescending(o => o.OrderDate)
                                        .ToListAsync();

            ViewBag.OrderHistory = orderHistory;
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Không tìm thấy người dùng.");
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null) return NotFound("Không tìm thấy đơn hàng.");

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id, string cancellationReason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
            if (order == null) return NotFound("Đơn hàng không tồn tại.");

            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Đơn hàng không thể hủy.";
                return RedirectToAction("OrderDetail", new { id = id });
            }

            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = cancellationReason;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đơn hàng đã được hủy thành công.";
            return RedirectToAction("Index");
        }

        // ==========================================
        // CHỨC NĂNG MỚI: XÓA TÀI KHOẢN
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // 1. Xử lý các đơn hàng liên quan (để tránh lỗi khóa ngoại trong Database)
            // Chúng ta gán UserId = null để đơn hàng đó trở thành "Khách vãng lai"
            var orders = await _db.Orders.Where(o => o.UserId == user.Id).ToListAsync();
            foreach (var order in orders)
            {
                order.UserId = null;
            }
            await _db.SaveChangesAsync();

            // 2. Thực hiện xóa User
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                // 3. Đăng xuất ngay lập tức sau khi xóa thành công
                await _signInManager.SignOutAsync();
                TempData["SuccessMessage"] = "Tài khoản của bạn đã được xóa thành công.";
                return RedirectToAction("Index", "Home");
            }

            TempData["ErrorMessage"] = "Không thể xóa tài khoản lúc này.";
            return RedirectToAction("Edit");
        }
    }
}