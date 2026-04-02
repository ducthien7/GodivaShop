using GodivaShop.Web.Models.Domain;
using GodivaShop.Web.Services; // Thêm dòng này để gọi EmailService
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GodivaShop.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService; // Khai báo EmailService

        public AccountController(UserManager<ApplicationUser> userManager, EmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService; // Tiêm (Inject) Service vào
        }

        // ĐÃ XÓA HÀM SendEmailAsync() PRIVATE ĐỂ DÙNG CHUNG SERVICE

        // 1. HIỂN THỊ TRANG NHẬP EMAIL QUÊN MẬT KHẨU
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // 2. XỬ LÝ KHI BẤM NÚT GỬI MÃ KHÔI PHỤC
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Vui lòng nhập Email.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // Tạo token và link
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account",
                    new { email = user.Email, token = token }, protocol: Request.Scheme);

                // Gửi thư thật
                string mailBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px;'>
                        <h2 style='color: #C1A35E;'>Godiva Shop - Khôi phục mật khẩu</h2>
                        <p>Chào bạn,</p>
                        <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Vui lòng click vào nút bên dưới để tạo mật khẩu mới:</p>
                        <a href='{callbackUrl}' style='display: inline-block; padding: 10px 20px; background-color: #1a1a1a; color: #fff; text-decoration: none; margin-top: 15px;'>ĐỔI MẬT KHẨU NGAY</a>
                        <p style='margin-top: 20px; font-size: 12px; color: #888;'>Nếu bạn không yêu cầu điều này, vui lòng bỏ qua email này.</p>
                    </div>";

                // Gọi EmailService dùng chung để gửi mail
                await _emailService.SendEmailAsync(email, "Khôi phục mật khẩu - Godiva Shop", mailBody);
            }

            ViewBag.Message = "Nếu Email này tồn tại trong hệ thống, một liên kết khôi phục đã được gửi đi. Vui lòng kiểm tra hộp thư của bạn (bao gồm cả thư rác).";
            return View();
        }

        // 3. HIỂN THỊ TRANG ĐỔI MẬT KHẨU MỚI
        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null) return BadRequest("Mã không hợp lệ.");
            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        // 4. LƯU MẬT KHẨU MỚI
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            ViewBag.Token = token;
            ViewBag.Email = email;

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return RedirectToAction("ResetPasswordConfirmation");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại!";
                return Redirect("/Identity/Account/Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View();
        }

        // 5. HÀM GỬI MAIL CHÀO MỪNG (Dùng cho bên trang Đăng ký)
        public async Task SendWelcomeEmail(string email, string fullName)
        {
            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color:#C1A35E;'>Chào mừng {fullName} đến với Godiva!</h2>
                    <p>Cảm ơn bạn đã gia nhập đại gia đình socola thượng hạng của chúng tôi.</p>
                    <p>Hãy khám phá những bộ sưu tập mới nhất ngay nhé!</p>
                    <a href='https://localhost:7105/' style='display: inline-block; padding:10px 20px; background:#C1A35E; color:#fff; text-decoration:none; margin-top:15px;'>MUA SẮM NGAY</a>
                </div>";

            // Gọi EmailService dùng chung để gửi mail
            await _emailService.SendEmailAsync(email, "Chào mừng thành viên mới - Godiva Shop", body);
        }
    }
}