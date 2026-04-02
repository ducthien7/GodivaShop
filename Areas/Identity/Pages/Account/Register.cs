using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace GodivaShop.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public RegisterModel(UserManager<ApplicationUser> userManager,
                             SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }
        public IList<AuthenticationScheme>? ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [StringLength(100, ErrorMessage = "Mật khẩu tối thiểu {2} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager
                .GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (!ModelState.IsValid) return Page();

            var fullName = Request.Form["FullName"].ToString();
            var phone = Request.Form["Phone"].ToString();

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FullName = string.IsNullOrEmpty(fullName) ? "Khách hàng" : fullName,
                //Phone = phone, // Bỏ comment dòng này nếu ApplicationUser của bạn có cột Phone
                CreatedAt = DateTime.Now,
                EmailConfirmed = true   // Bỏ qua xác nhận email cho đơn giản
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                // Gán role Customer mặc định
                await _userManager.AddToRoleAsync(user, "Customer");

                // ==========================================
                // CHÈN HÀM GỬI MAIL CHÀO MỪNG TẠI ĐÂY
                // ==========================================
                try
                {
                    await SendWelcomeEmail(user.Email, user.FullName);
                }
                catch
                {
                    // Bọc trong try-catch để lỡ mạng lag gửi mail lỗi thì web vẫn cho khách đăng nhập bình thường
                }
                // ==========================================

                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            // Hiển thị lỗi (password quá yếu, email đã tồn tại, v.v.)
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return Page();
        }

        // ==========================================
        // BỘ MÁY GỬI EMAIL CHÀO MỪNG
        // ==========================================
        private async Task SendWelcomeEmail(string toEmail, string fullName)
        {
            // BẠN NHỚ THAY EMAIL VÀ MẬT KHẨU ỨNG DỤNG VÀO 2 DÒNG NÀY NHÉ
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

            string body = $@"
                <div style='font-family: Arial, sans-serif; padding: 30px; border: 1px solid #e0e0e0; max-width: 600px; margin: 0 auto;'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <h1 style='color: #C1A35E; font-family: ""Playfair Display"", serif; letter-spacing: 3px;'>GODIVA</h1>
                    </div>
                    <h2 style='color: #1a1a1a;'>Chào mừng {fullName} đến với thế giới socola!</h2>
                    <p style='color: #555; line-height: 1.6;'>Cảm ơn bạn đã đăng ký tài khoản thành công. Kể từ bây giờ, bạn đã chính thức trở thành một phần của đại gia đình Godiva.</p>
                    <p style='color: #555; line-height: 1.6;'>Hãy đăng nhập ngay để theo dõi lịch sử mua hàng, lưu địa chỉ giao hàng và nhận những ưu đãi độc quyền dành riêng cho thành viên.</p>
                    <div style='text-align: center; margin-top: 30px;'>
                        <a href='https://localhost:7105/' style='display: inline-block; padding: 12px 30px; background-color: #C1A35E; color: #ffffff; text-decoration: none; font-weight: bold; letter-spacing: 1px; text-transform: uppercase; font-size: 14px;'>Khám phá ngay</a>
                    </div>
                </div>";

            using var message = new MailMessage(new MailAddress(fromEmail, "Godiva Shop"), new MailAddress(toEmail))
            {
                Subject = "Chào mừng thành viên mới - Godiva Shop",
                Body = body,
                IsBodyHtml = true
            };

            await smtp.SendMailAsync(message);
        }
    }
}