using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;

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
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Path = "/"
                });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost("language")]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage([FromForm] string culture, [FromForm] string? returnUrl)
        {
            culture = (culture ?? "").Trim().ToLowerInvariant();

            if (culture != "en" && culture != "ru")
                culture = "en";

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps,
                    Path = "/"
                });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}
