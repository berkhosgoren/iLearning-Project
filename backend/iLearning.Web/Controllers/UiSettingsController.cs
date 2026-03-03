using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;

namespace iLearning.Web.Controllers
{
    [Route("ui")]
    public class UiSettingsController : Controller
    {
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
