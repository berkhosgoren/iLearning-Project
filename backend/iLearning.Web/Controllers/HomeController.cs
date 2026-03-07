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
            var latestRows = await _db.Inventories
                .AsNoTracking()
                .Where(i => i.IsPublic)
                .Include(i => i.Category)
                .Include(i => i.Creator)
                .OrderByDescending(i => i.CreatedAtUtc)
                .Take(12)
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

            var latest = latestRows
                .Select(i => new HomeInventoryCardVm
                {
                    Id = i.Id,
                    Title = i.Title,
                    CategoryName = i.CategoryName,
                    CreatorName = i.CreatorName,
                    CreatedAtUtc = i.CreatedAtUtc,
                    ImageUrl = i.ImageUrl,
                    DescriptionHtml = _markdown.ToSafeHtml(i.Description),
                    ActivityCount = 0,
                    LastActivityAtUtc = null
                })
                .ToList();

            var popularRows = await _db.Inventories
                .AsNoTracking()
                .Where(i => i.IsPublic)
                .Include(i => i.Category)
                .Include(i => i.Creator)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    CategoryName = i.Category != null ? i.Category.Name : "Other",
                    CreatorName = i.Creator != null ? i.Creator.Name : "Unknown",
                    i.CreatedAtUtc,
                    i.ImageUrl,
                    i.Description,

                    InventoryDiscussionCount = i.DiscussionComments.Count,
                    InventoryDiscussionLastAt = i.DiscussionComments
                        .OrderByDescending(x => x.CreatedAtUtc)
                        .Select(x => (DateTime?)x.CreatedAtUtc)
                        .FirstOrDefault(),

                    ItemCommentsCount = i.Items.SelectMany(x => x.Comments).Count(),
                    ItemCommentsLastAt = i.Items
                        .SelectMany(x => x.Comments)
                        .OrderByDescending(x => x.CreatedAtUtc)
                        .Select(x => (DateTime?)x.CreatedAtUtc)
                        .FirstOrDefault(),

                    ItemLikesCount = i.Items.SelectMany(x => x.Likes).Count(),
                    ItemLikesLastAt = i.Items
                        .SelectMany(x => x.Likes)
                        .OrderByDescending(x => x.CreatedAtUtc)
                        .Select(x => (DateTime?)x.CreatedAtUtc)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var popular = popularRows
                .Select(i =>
                {
                    var activityCount = i.InventoryDiscussionCount + i.ItemCommentsCount + i.ItemLikesCount;

                    DateTime? lastActivityAtUtc = new[]
                    {
                        i.InventoryDiscussionLastAt,
                        i.ItemCommentsLastAt,
                        i.ItemLikesLastAt
                    }
                    .Where(x => x.HasValue)
                    .Select(x => x!.Value)
                    .DefaultIfEmpty()
                    .Max();

                    return new HomeInventoryCardVm
                    {
                        Id = i.Id,
                        Title = i.Title,
                        CategoryName = i.CategoryName,
                        CreatorName = i.CreatorName,
                        CreatedAtUtc = i.CreatedAtUtc,
                        ImageUrl = i.ImageUrl,
                        DescriptionHtml = _markdown.ToSafeHtml(i.Description),
                        ActivityCount = activityCount,
                        LastActivityAtUtc = lastActivityAtUtc == default ? null : lastActivityAtUtc
                    };
                })
                .Where(x => x.ActivityCount > 0)
                .OrderByDescending(x => x.ActivityCount)
                .ThenByDescending(x => x.LastActivityAtUtc)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Take(12)
                .ToList();

            var vm = new HomeIndexVm
            {
                LatestInventories = latest,
                PopularInventories = popular
            };

            return View(vm);
        }
    }
}
