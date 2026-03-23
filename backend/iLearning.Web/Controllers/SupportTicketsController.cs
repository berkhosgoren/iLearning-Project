using Microsoft.AspNetCore.Mvc;

namespace iLearning.Web.Controllers
{
    public class SupportTicketsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
