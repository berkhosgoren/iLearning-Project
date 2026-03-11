using iLearning.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Services;
using iLearning.Web.Models.ViewModels.Home;
using iLearning.Web;
using Microsoft.Extensions.Localization;


namespace iLearning.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IMarkdownService _markdown;
        private readonly IStringLocalizer<SharedResource> T;

        public HomeController(AppDbContext db, IMarkdownService markdown, IStringLocalizer<SharedResource> t)
        {
            _db = db;
            _markdown = markdown;
            T = t;
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
                    CategoryName = i.Category != null ? i.Category.Name : T["Common.Other"].Value,
                    CreatorName = i.Creator != null ? i.Creator.Name : T["Common.Unknown"].Value,
                    i.CreatedAtUtc,
                    i.ImageUrl,
                    i.Description
                })
                .ToListAsync();

            var latest = latestRows
                .Select(i =>
                {
                    var safeHtml = _markdown.ToSafeHtml(i.Description);
                    var previewText = _markdown.ToPreviewText(i.Description, 160);

                    return new HomeInventoryCardVm
                    {
                        Id = i.Id,
                        Title = i.Title,
                        CategoryName = i.CategoryName,
                        CreatorName = i.CreatorName,
                        CreatedAtUtc = i.CreatedAtUtc,
                        ImageUrl = i.ImageUrl,
                        DescriptionHtml = safeHtml,
                        DescriptionPreview = previewText,
                        ActivityCount = 0,
                        ActivityScore = 0,
                        LastActivityAtUtc = null
                    };
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
                    CategoryName = i.Category != null ? i.Category.Name : T["Common.Other"].Value,
                    CreatorName = i.Creator != null ? i.Creator.Name : T["Common.Unknown"].Value,
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
                    var safeHtml = _markdown.ToSafeHtml(i.Description);
                    var previewText = _markdown.ToPreviewText(i.Description, 160);

                    var activityCount = i.InventoryDiscussionCount + i.ItemCommentsCount + i.ItemLikesCount;

                    var activityScore =
                        (i.InventoryDiscussionCount * 3) +
                        (i.ItemCommentsCount * 2) +
                        (i.ItemLikesCount * 1);

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
                        DescriptionHtml = safeHtml,
                        DescriptionPreview = previewText,
                        ActivityCount = activityCount,
                        ActivityScore = activityScore,
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
