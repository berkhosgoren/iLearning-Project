using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Data;
using iLearning.Web.Services;
using iLearning.Web.Models.ViewModels.Inventories;
using Microsoft.Extensions.Localization;
using System.Text;

namespace iLearning.Web.Controllers
{
    [Authorize]
    [Route("inventories/{inventoryId:guid}/stats")]
    public class InventoryStatsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;
        private readonly IStringLocalizer<SharedResource> T;

        public InventoryStatsController(AppDbContext db, CurrentUserService current, IStringLocalizer<SharedResource> t)
        {
            _db = db;
            _current = current;
            T = t;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid inventoryId)
        {
            var vm = await BuildStatsPageVmAsync(inventoryId);
            if (vm == null)
                return await BuildStatsAccessResultAsync(inventoryId);

            return View(vm);
        }

        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportCsv(Guid inventoryId)
        {
            var vm = await BuildStatsPageVmAsync(inventoryId);
            if (vm == null)
                return await BuildStatsAccessResultAsync(inventoryId);

            var csv = BuildCsv(vm);
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();

            var safeTitle = SanitizeFileName(vm.InventoryTitle);
            var fileName = $"inventory-stats-{safeTitle}.csv";

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }

        private async Task<InventoryStatsPageVm?> BuildStatsPageVmAsync(Guid inventoryId)
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return null;

            var isAdmin = _current.IsAdmin(User);

            var inv = await _db.Inventories
                .AsNoTracking()
                .Select(i => new { i.Id, i.Title, i.CreatorId, i.IsPublic })
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return null;

            var isOwner = inv.CreatorId == userId.Value;
            var canEdit = isAdmin || isOwner;

            if (!canEdit) return null;

            var itemsQuery = _db.Items
                .AsNoTracking()
                .Where(x => x.InventoryId == inventoryId);

            var itemsTotal = await itemsQuery.CountAsync();

            var likesTotal = await _db.ItemLikes
                .AsNoTracking()
                .CountAsync(l => l.Item.InventoryId == inventoryId);

            var commentsTotal = await _db.ItemComments
                .AsNoTracking()
                .CountAsync(c => c.Item.InventoryId == inventoryId);

            var lastItemCreatedAtUtc = await itemsQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => (DateTime?)x.CreatedAtUtc)
                .FirstOrDefaultAsync();

            var lastItemUpdatedAtUtc = await itemsQuery
                .Where(x => x.UpdatedAtUtc.HasValue)
                .OrderByDescending(x => x.UpdatedAtUtc)
                .Select(x => x.UpdatedAtUtc)
                .FirstOrDefaultAsync();

            var topByLikes = await itemsQuery
                .OrderByDescending(x => x.Likes.Count)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Take(10)
                .Select(x => new TopItemVm
                {
                    ItemId = x.Id,
                    CustomId = x.CustomId,
                    Title = x.Title,
                    LikesCount = x.Likes.Count,
                    CommentsCount = x.Comments.Count
                })
                .ToListAsync();

            var topByComments = await itemsQuery
                .OrderByDescending(x => x.Comments.Count)
                .ThenByDescending(x => x.CreatedAtUtc)
                .Take(10)
                .Select(x => new TopItemVm
                {
                    ItemId = x.Id,
                    CustomId = x.CustomId,
                    Title = x.Title,
                    LikesCount = x.Likes.Count,
                    CommentsCount = x.Comments.Count
                })
                .ToListAsync();

            return new InventoryStatsPageVm
            {
                InventoryId = inv.Id,
                InventoryTitle = inv.Title,
                IsPublic = inv.IsPublic,
                CanEdit = canEdit,
                CanWrite = canEdit,

                Stats = new InventoryStatsVm
                {
                    InventoryId = inv.Id,
                    ItemsTotal = itemsTotal,
                    LikesTotal = likesTotal,
                    CommentsTotal = commentsTotal,
                    LastItemCreatedAtUtc = lastItemCreatedAtUtc,
                    LastItemUpdatedAtUtc = lastItemUpdatedAtUtc,
                    TopItemsByLikes = topByLikes,
                    TopItemsByComments = topByComments
                }
            };
        }

        private async Task<IActionResult> BuildStatsAccessResultAsync(Guid inventoryId)
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var inv = await _db.Inventories
                .AsNoTracking()
                .Select(i => new { i.Id, i.CreatorId })
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null)
                return NotFound();

            var isAdmin = _current.IsAdmin(User);
            var isOwner = inv.CreatorId == userId.Value;

            if (!isAdmin && !isOwner)
                return Forbid();

            return NotFound();
        }

        private string BuildCsv(InventoryStatsPageVm vm)
        {
            var sb = new StringBuilder();

            void AppendRow(params string[] values)
            {
                sb.AppendLine(string.Join(",", values.Select(EscapeCsv)));
            }

            AppendRow(T["InventoryStats.InventoryStats"].Value, vm.InventoryTitle);
            AppendRow(T["InventoryStats.Csv.GeneratedAt"].Value, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"));
            AppendRow(T["Common.Visibility"].Value, vm.IsPublic ? T["Common.Public"].Value : T["Common.Private"].Value);
            AppendRow();

            AppendRow(T["InventoryStats.Csv.Summary"].Value);
            AppendRow(T["InventoryStats.Csv.Metric"].Value, T["InventoryStats.Csv.Value"].Value);
            AppendRow(T["Common.Items"].Value, vm.Stats.ItemsTotal.ToString());
            AppendRow(T["Common.Likes"].Value, vm.Stats.LikesTotal.ToString());
            AppendRow(T["Common.Comments"].Value, vm.Stats.CommentsTotal.ToString());
            AppendRow(T["InventoryStats.LastItemCreated"].Value, FormatDate(vm.Stats.LastItemCreatedAtUtc));
            AppendRow(T["InventoryStats.LastItemUpdated"].Value, FormatDate(vm.Stats.LastItemUpdatedAtUtc));
            AppendRow();

            AppendRow(T["InventoryStats.TopItemsByLikes"].Value);
            AppendRow(T["Common.CustomId"].Value, T["Common.Title"].Value, T["Common.Likes"].Value, T["Common.Comments"].Value);
            foreach (var item in vm.Stats.TopItemsByLikes)
            {
                AppendRow(
                    item.CustomId,
                    item.Title,
                    item.LikesCount.ToString(),
                    item.CommentsCount.ToString());
            }

            AppendRow();

            AppendRow(T["InventoryStats.TopItemsByComments"].Value);
            AppendRow(T["Common.CustomId"].Value, T["Common.Title"].Value, T["Common.Likes"].Value, T["Common.Comments"].Value);
            foreach (var item in vm.Stats.TopItemsByComments)
            {
                AppendRow(
                    item.CustomId,
                    item.Title,
                    item.LikesCount.ToString(),
                    item.CommentsCount.ToString());
            }

            return sb.ToString();
        }

        private static string EscapeCsv(string? value)
        {
            var text = value ?? string.Empty;

            if (text.Contains('"'))
                text = text.Replace("\"", "\"\"");

            if (text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains("\r"))
                text = $"\"{text}\"";

            return text;
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") : string.Empty;
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "inventory";

            var invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(value.Where(c => !invalidChars.Contains(c)).ToArray()).Trim();

            if (string.IsNullOrWhiteSpace(cleaned))
                return "inventory";

            return cleaned.Length > 80 ? cleaned[..80] : cleaned;
        }
    }
}
