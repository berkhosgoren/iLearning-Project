using Microsoft.AspNetCore.Mvc;

namespace iLearning.Web.Controllers
{
    [Route("prefs")]
    public class PreferencesController : Controller
    {
        [HttpPost("theme")]
        [ValidateAntiForgeryToken]
        public IActionResult SetTheme([FromForm] string theme, [FromForm] string? returnUrl)
        {
            theme = (theme ?? "").Trim().ToLowerInvariant();
            if (theme != "light" && theme != "dark")
                theme = "light";

            Response.Cookies.Append(
                "ilearning_theme",
                theme,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Path = "/"
                });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}
