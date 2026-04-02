using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GodivaShop.Web.Areas.Identity.Pages.Account;

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
            Phone = phone,
            CreatedAt = DateTime.Now,
            EmailConfirmed = true   // Bỏ qua xác nhận email cho đơn giản
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            // Gán role Customer mặc định
            await _userManager.AddToRoleAsync(user, "Customer");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        // Hiển thị lỗi (password quá yếu, email đã tồn tại, v.v.)
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        

        return Page();
    }
}