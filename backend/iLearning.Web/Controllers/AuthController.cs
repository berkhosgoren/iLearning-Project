using System.Security.Claims;
using iLearning.Web.Data;
using iLearning.Web.Models.Domain;
using iLearning.Web.Security;
using iLearning.Web.Models.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;


namespace iLearning.Web.Controllers
{
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IStringLocalizer<SharedResource> T;

        public AuthController(AppDbContext db, IStringLocalizer<SharedResource> t)
        {
            _db = db;
            T = t;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return View(new LoginVm());
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var email = (vm.Email ?? "").Trim().ToLowerInvariant();
            var password = vm.Password ?? "";

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null || user.PasswordHash is null || string.IsNullOrWhiteSpace(vm.Password) 
                || !PasswordHasher.Verify(vm.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", T["Auth.Errors.InvalidCredentials"]);
                return View(vm);
            }

            if (user.IsBlocked)
            {
                ModelState.AddModelError("", T["Auth.Errors.UserBlocked"]);
                return View(vm); 
            }

            await SignInUserAsync(user, vm.RememberMe);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            return View(new RegisterVm());
        }

        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var name = (vm.Name ?? "").Trim();
            var email = (vm.Email ?? "").Trim().ToLowerInvariant();
            var password = vm.Password ?? "";

            var exists = await _db.Users.AnyAsync(u => u.Email == email);
            if (exists)
            {
                ModelState.AddModelError(nameof(vm.Email), T["Auth.Errors.EmailExists"]);
                return View(vm);
            }

            var user = new AppUser
            {
                Name = name,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                IsBlocked = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            TempData["Message"] = T["Auth.Register.Success"].Value;
            return RedirectToAction(nameof(Login));
        }

        [HttpPost("external/{provider}")]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            if (provider != "Google" && provider != "GitHub")
                return RedirectToAction(nameof(Login));

            var redirectUrl = Url.Action(nameof(ExternalCallback), "Auth", new { provider, returnUrl });

            var props = new AuthenticationProperties
            {
                RedirectUri = redirectUrl ?? "/"
            };

            return Challenge(props, provider);
        }

        [HttpGet("external/callback")]
        public async Task<IActionResult> ExternalCallback(string provider, string? returnUrl = null, string? remoteError = null)
        {
            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                TempData["Message"] = T["Auth.External.RemoteError"].Value;
                return RedirectToAction(nameof(Login));
            }

            var result = await HttpContext.AuthenticateAsync("External");
            if (!result.Succeeded || result.Principal is null)
            {
                TempData["Message"] = T["Auth.External.Failed"].Value;
                return RedirectToAction(nameof(Login));
            }

            var principal = result.Principal;

            var externalUserId =
                principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
                principal.FindFirstValue("sub") ??
                principal.FindFirstValue("id");

            var email =
                principal.FindFirstValue(ClaimTypes.Email) ??
                principal.FindFirstValue("email");

            var name =
                principal.FindFirstValue(ClaimTypes.Name) ??
                principal.FindFirstValue("name");

            if (string.IsNullOrWhiteSpace(externalUserId))
            {
                await HttpContext.SignOutAsync("External");
                TempData["Message"] = T["Auth.External.MissingId"].Value;
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                await HttpContext.SignOutAsync("External");
                TempData["Message"] = T["Auth.External.MissingEmail"].Value;
                return RedirectToAction(nameof(Login));
            }

            email = email.Trim().ToLowerInvariant();
            name = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim();

            var user = await _db.Users.FirstOrDefaultAsync(u =>
                u.ExternalProvider == provider &&
                u.ExternalProviderUserId == externalUserId);

            if (user is null)
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user is null)
                {
                    user = new AppUser
                    {
                        Name = name,
                        Email = email,
                        IsBlocked = false,
                        PasswordHash = null,
                        ExternalProvider = provider,
                        ExternalProviderUserId = externalUserId,
                    };

                    _db.Users.Add(user);
                }
                else
                {
                    user.ExternalProvider ??= provider;
                    user.ExternalProviderUserId ??= externalUserId;

                    if (string.IsNullOrWhiteSpace(user.Name))
                        user.Name = name;
                }

                await _db.SaveChangesAsync();
            }

            if (user.IsBlocked)
            {
                await HttpContext.SignOutAsync("External");
                TempData["Message"] = T["Auth.Errors.UserBlocked"].Value;
                return RedirectToAction(nameof(Login));
            }

            await HttpContext.SignOutAsync("External");
            await SignInUserAsync(user, true);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("denied")]
        public IActionResult Denied()
        {
            return View();
        }

        private async Task SignInUserAsync(AppUser user, bool isPersistent)
        {
            var roleNames = await _db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            foreach (var role in roleNames)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = isPersistent
                });
        }
    }
}
