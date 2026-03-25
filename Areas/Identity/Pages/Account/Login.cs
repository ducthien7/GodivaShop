using GodivaShop.Web.Models.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GodivaShop.Web.Areas.Identity.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public LoginModel(SignInManager<ApplicationUser> signInManager,
                      UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
            ModelState.AddModelError(string.Empty, ErrorMessage);

        ReturnUrl = returnUrl ?? Url.Content("~/");
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ExternalLogins = (await _signInManager
            .GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await _signInManager
            .GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid) return Page();

        var result = await _signInManager.PasswordSignInAsync(
            Input.Email, Input.Password,
            Input.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            // Nếu là Admin/Staff → redirect sang Admin
            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin") || roles.Contains("Staff"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản bị khóa. Thử lại sau.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
        return Page();
    }
}