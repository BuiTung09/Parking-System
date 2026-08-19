using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Models;
using ParkingSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace ParkingSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == model.Username || u.PhoneNumber == model.Username);
                if (user != null && !user.IsActive)
                {
                    _logger.LogWarning("[LOGIN FAILED] Tài khoản {Username} đã bị khóa.", model.Username);
                    ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Liên hệ Admin.");
                    return View(model);
                }

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác!");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("[LOGIN SUCCESS] Người dùng {Username} đăng nhập thành công.", model.Username);
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin")) return RedirectToAction("Index", "Admin");
                    if (roles.Contains("Staff")) return RedirectToAction("Index", "Staff");
                    if (roles.Contains("Member")) return RedirectToAction("Index", "Member");
                    return RedirectToLocal(returnUrl);
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("[LOGIN LOCKED] Tài khoản {Username} bị khóa do đăng nhập sai nhiều lần.", model.Username);
                    ModelState.AddModelError(string.Empty, "Tài khoản bị khóa tạm thời. Vui lòng thử lại sau.");
                }
                else
                {
                    _logger.LogWarning("[LOGIN FAILED] Đăng nhập thất bại cho {Username}.", model.Username);
                    ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác!");
                }
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.PhoneNumber,
                    Email = $"{model.PhoneNumber}@guest.local",
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    IsActive = true,
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Member");
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("[REGISTER SUCCESS] Thành viên mới {Phone} đăng ký thành công.", model.PhoneNumber);
                    return RedirectToAction("Index", "Member");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var email = User.Identity?.Name;
            await _signInManager.SignOutAsync();
            _logger.LogInformation("[LOGOUT] Người dùng {Email} đã đăng xuất.", email);
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Login");
        }
    }
}
