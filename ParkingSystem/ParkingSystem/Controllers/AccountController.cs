using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using ParkingSystem.Models;
using ParkingSystem.Models.ViewModels;
using System.Text;

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
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.Email == model.Username || u.PhoneNumber == model.Username);

            if (user == null
                || !string.Equals(user.FullName.Trim(), model.FullName.Trim(), StringComparison.CurrentCultureIgnoreCase))
            {
                _logger.LogWarning("[FORGOT PASSWORD] Không xác minh được tài khoản {Username}.", model.Username);
                ModelState.AddModelError(string.Empty, "Không tìm thấy tài khoản khớp với số điện thoại/email và họ tên đã nhập.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Liên hệ Admin để được hỗ trợ.");
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            _logger.LogInformation("[FORGOT PASSWORD] {User} xác minh thành công, chuyển tới đặt lại mật khẩu.", user.FullName);
            return RedirectToAction(nameof(ResetPassword), new { userId = user.Id, token = encodedToken });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction(nameof(ForgotPassword));

            return View(new ResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Không thể đặt lại mật khẩu. Vui lòng thử lại.");
                return View(model);
            }

            string decodedToken;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Liên kết đặt lại mật khẩu không hợp lệ.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("[RESET PASSWORD] {User} đã đặt lại mật khẩu thành công.", user.FullName);
                TempData["Success"] = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập bằng mật khẩu mới.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

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
