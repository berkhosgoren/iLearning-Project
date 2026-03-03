using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Data;
using iLearning.Web.Services;
using iLearning.Web.Models.ViewModels.Inventories;
using Microsoft.Extensions.Localization;

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
            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            var isAdmin = _current.IsAdmin(User);

            var inv = await _db.Inventories
                .AsNoTracking()
                .Select(i => new { i.Id, i.Title, i.CreatorId, i.IsPublic })
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return NotFound();

            var isOwner = inv.CreatorId == userId.Value;
            var canEdit = isAdmin || isOwner;

            if (!canEdit) return Forbid();

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

            var vm = new InventoryStatsPageVm
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

            return View(vm);
        }
    }
    
}
