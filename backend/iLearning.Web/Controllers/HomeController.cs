using iLearning.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Services;
using iLearning.Web.Models.ViewModels.Home;


namespace iLearning.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMarkdownService _markdown;

        public HomeController(AppDbContext db, IMarkdownService markdown)
        {
            _db = db;
            _markdown = markdown;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var rows = await _db.Inventories
                .AsNoTracking()
                .Where(i => i.IsPublic)
                .Include(i => i.Category)
                .Include(i => i.Creator)
                .OrderByDescending(i => i.CreatedAtUtc)
                .Take(60)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    CategoryName = i.Category != null ? i.Category.Name : "Other",
                    CreatorName = i.Creator != null ? i.Creator.Name : "Unknown",
                    i.CreatedAtUtc,
                    i.ImageUrl,
                    i.Description
                })
                .ToListAsync();

            var vm = rows.Select(i => new HomeInventoryCardVm
            {
                Id = i.Id,
                Title = i.Title,
                CategoryName = i.CategoryName,
                CreatorName = i.CreatorName,
                CreatedAtUtc = i.CreatedAtUtc,
                ImageUrl = i.ImageUrl,
                DescriptionHtml = _markdown.ToSafeHtml(i.Description)
            })
                .ToList();

            return View(vm);
        }
    }
}
