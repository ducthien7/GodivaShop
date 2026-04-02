using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers
{
    [Authorize] // Bắt buộc phải đăng nhập mới được vào trang này
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Lấy thông tin user đang đăng nhập
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("Không tìm thấy thông tin người dùng.");
            }

            // 2. Lấy lịch sử đơn hàng của user này (Sắp xếp từ mới nhất đến cũ nhất)
            // Lưu ý: Đổi chữ 'Orders' thành tên bảng đơn hàng thực tế của bạn trong DbContext
            var orderHistory = await _db.Orders
                                        .Where(o => o.UserId == user.Id)
                                        .OrderByDescending(o => o.OrderDate)
                                        .ToListAsync();

            // Nhét danh sách đơn hàng vào ViewBag để truyền sang View
            ViewBag.OrderHistory = orderHistory;

            // Truyền thông tin User sang View
            return View(user);
        }

        // Hiển thị form cập nhật
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            return View(user);
        }

        // Xử lý khi khách hàng bấm nút Lưu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // Ghi đè thông tin mới vào thông tin cũ
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            // Lưu vào Database
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Index"); // Quay lại trang hồ sơ
            }

            ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu.");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            // Tìm đơn hàng theo ID. 
            // Lưu ý BẢO MẬT: Phải check thêm UserId để tránh việc khách này xem lén đơn của khách khác
            var order = await _db.Orders
                .Include(o => o.OrderItems)           // Lấy danh sách các món trong đơn
                .ThenInclude(oi => oi.Product)        // Lấy luôn tên sản phẩm của từng món
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng hoặc bạn không có quyền xem đơn này.");
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id, string cancellationReason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Tìm đơn hàng của đúng User đó
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null) return NotFound("Đơn hàng không tồn tại.");

            // Kiểm tra điều kiện: Chỉ được hủy khi trạng thái là Pending (Chờ xác nhận)
            if (order.Status != OrderStatus.Pending)
            {
                TempData["Error"] = "Đơn hàng đã được xác nhận hoặc đang giao, không thể hủy.";
                return RedirectToAction("OrderDetail", new { id = id });
            }

            // Cập nhật trạng thái và lý do
            order.Status = OrderStatus.Cancelled;
            order.CancellationReason = cancellationReason;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Đơn hàng đã được hủy thành công.";
            return RedirectToAction("Index");
        }
    }
}