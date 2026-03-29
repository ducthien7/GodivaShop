using GodivaShop.Web.Data;
using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GodivaShop.Web.Controllers
{
    public class GiftController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public GiftController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // 1. Trang hiển thị Vòng quay
        public IActionResult Index()
        {
            return View();
        }

        // 2. API lấy danh sách các ô (để JS vẽ vòng quay)
        [HttpGet]
        public async Task<IActionResult> GetPrizes()
        {
            var prizes = await _db.LuckyPrizes
                .Where(p => p.IsActive && (p.Quantity > 0 || p.Quantity == -1))
                .Select(p => new { p.Id, p.Name, p.FillColor })
                .ToListAsync();
            return Json(prizes);
        }

        // 3. API xử lý hành động QUAY (Bắt buộc đăng nhập)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Spin()
        {
            var userId = _userManager.GetUserId(User);
            var today = DateTime.Today;

            // Ktra bảo mật: Khách đã quay hôm nay chưa?
            var hasSpunToday = await _db.UserSpinHistories
                .AnyAsync(h => h.UserId == userId && h.SpinDate.Date == today);

            if (hasSpunToday)
            {
                return Json(new { success = false, message = "Bạn đã hết lượt quay hôm nay. Hãy quay lại vào ngày mai nhé!" });
            }

            // Lấy danh sách giải thưởng hợp lệ
            var prizes = await _db.LuckyPrizes
                .Where(p => p.IsActive && (p.Quantity > 0 || p.Quantity == -1))
                .ToListAsync();

            if (!prizes.Any())
            {
                return Json(new { success = false, message = "Vòng quay đang được bảo trì, vui lòng quay lại sau." });
            }

            // --- THUẬT TOÁN CHỌN QUÀ NGẪU NHIÊN THEO TỶ LỆ (Weighted Random) ---
            double totalChance = prizes.Sum(p => p.WinChance);
            Random random = new Random();
            double randomNumber = random.NextDouble() * totalChance; // Random từ 0 đến tổng %

            LuckyPrize selectedPrize = null;
            double accumulatedChance = 0;

            foreach (var prize in prizes)
            {
                accumulatedChance += prize.WinChance;
                if (randomNumber <= accumulatedChance)
                {
                    selectedPrize = prize;
                    break;
                }
            }
            // -----------------------------------------------------------------

            if (selectedPrize == null) selectedPrize = prizes.First(); // Dự phòng

            // --- XỬ LÝ PHẦN THƯỞNG ---
            string generatedCode = null;

            // Nếu trúng Voucher (DiscountValue có giá trị)
            // Nếu trúng Voucher (DiscountValue có giá trị)
            if (selectedPrize.DiscountValue.HasValue)
            {
                generatedCode = "LUCKY-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                // KIỂM TRA TỰ ĐỘNG: Nếu giá trị <= 100 thì là %, lớn hơn thì là tiền mặt (VND)
                var currentDiscountType = selectedPrize.DiscountValue.Value <= 100
                    ? DiscountType.Percentage
                    : DiscountType.FixedAmount;

                // Tạo một Coupon mới vào bảng Coupons của bạn
                var coupon = new Coupon
                {
                    Code = generatedCode,
                    DiscountType = currentDiscountType, // <--- Đã sửa thành biến tự động
                    DiscountValue = selectedPrize.DiscountValue.Value,
                    IsActive = true,
                    ExpiryDate = DateTime.Now.AddDays(7) // Hạn dùng 7 ngày
                };
                _db.Coupons.Add(coupon);
            }

            // Trừ số lượng quà (nếu có giới hạn)
            if (selectedPrize.Quantity > 0)
            {
                selectedPrize.Quantity--;
            }

            // Lưu lịch sử
            var history = new UserSpinHistory
            {
                UserId = userId,
                PrizeId = selectedPrize.Id,
                GeneratedCode = generatedCode,
                SpinDate = DateTime.Now
            };
            _db.UserSpinHistories.Add(history);

            await _db.SaveChangesAsync();

            // Tính góc dừng cho JavaScript (Vòng quay chia đều N ô)
            int prizeIndex = prizes.IndexOf(selectedPrize);

            return Json(new
            {
                success = true,
                prizeName = selectedPrize.Name,
                prizeIndex = prizeIndex, // JS cần index này để biết dừng ở đâu
                code = generatedCode,
                message = generatedCode != null ? $"Chúc mừng! Bạn đã trúng {selectedPrize.Name}. Mã: <strong>{generatedCode}</strong> (Hạn dùng 7 ngày)" : $"Chúc mừng! Bạn đã trúng {selectedPrize.Name}."
            });
        }
    }
}