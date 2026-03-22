using Microsoft.AspNetCore.Mvc;

namespace iLearning.Web.Controllers
{
    public class SalesforceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
