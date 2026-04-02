using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GodivaShop.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // --- BỘ MÁY GỬI MAIL DÙNG CHUNG ---
        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            // BẠN NHỚ THAY EMAIL VÀ MẬT KHẨU ỨNG DỤNG 16 CHỮ CÁI VÀO ĐÂY NHÉ
            string fromEmail = "kietqq123@gmail.com";
            string fromPassword = "pdlp nupk mxgd gomn";
            
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail, fromPassword)
            };

            using var message = new MailMessage(new MailAddress(fromEmail, "Godiva Shop"), new MailAddress(toEmail))
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(message);
        }

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

                await SendEmailAsync(email, "Khôi phục mật khẩu - Godiva Shop", mailBody);
            }

            // Dù có tài khoản hay không cũng báo thành công để chống hacker dò email
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

            await SendEmailAsync(email, "Chào mừng thành viên mới - Godiva Shop", body);
        }
    }
}