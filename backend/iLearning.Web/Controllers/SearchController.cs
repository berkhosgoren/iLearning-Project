using Microsoft.AspNetCore.Mvc;

namespace iLearning.Web.Controllers
{
    public class SearchController : Controller
    {
        [HttpGet("/search")]
        public IActionResult Index([FromQuery] string ? q)
        {
            ViewBag.Query = q?.Trim() ?? "";

            return View();
        }
    }
}
