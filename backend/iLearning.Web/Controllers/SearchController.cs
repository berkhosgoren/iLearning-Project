using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Search;
using iLearning.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iLearning.Web.Controllers
{
    [AllowAnonymous]
    [Route("search")]
    public class SearchController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;

        public SearchController(AppDbContext db, CurrentUserService current)
        {
            _db = db;
            _current = current;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] string? q)
        {
            var vm = new SearchResultsVm
            {
                Q = (q ?? "").Trim()
            };

            if (string.IsNullOrWhiteSpace(vm.Q))
            {
                vm.IsAuthenticated = _current.IsAuthenticated(User);
                vm.IsAdmin = _current.IsAdmin(User);
                return View(vm);
            }

            var isAuthenticated = _current.IsAuthenticated(User);
            var isAdmin = _current.IsAdmin(User);
            var userId = _current.GetUserId(User);

            vm.IsAuthenticated = isAuthenticated;
            vm.IsAdmin = isAdmin;

            var pattern = $"%{vm.Q}%";

            IQueryable<Guid> allowedInventoryIds;

            if (isAdmin)
            {
                allowedInventoryIds = _db.Inventories.AsNoTracking().Select(i => i.Id);
            }
            else if (!isAuthenticated || !userId.HasValue)
            {
                allowedInventoryIds = _db.Inventories.AsNoTracking()
                    .Where(i => i.IsPublic)
                    .Select(i => i.Id);
            }
            else
            {
                var uid = userId.Value;

                var accessIds = _db.InventoryAccesses.AsNoTracking()
                    .Where(a => a.UserId == uid)
                    .Select(a => a.InventoryId);

                allowedInventoryIds = _db.Inventories.AsNoTracking()
                    .Where(i => i.IsPublic || i.CreatorId == uid || accessIds.Contains(i.Id))
                    .Select(i => i.Id);
            }

            vm.Inventories = await _db.Inventories
                .AsNoTracking()
                .Include(i => i.Category)
                .Include(i => i.Creator)
                .Where(i => allowedInventoryIds.Contains(i.Id))
                .Where(i => EF.Functions.ILike(i.Title, pattern) ||
                    (i.Description != null && EF.Functions.ILike(i.Description, pattern)) ||
                    (i.Category != null && EF.Functions.ILike(i.Category.Name, pattern)) ||
                    (i.Creator != null && EF.Functions.ILike(i.Creator.Name, pattern))
                )
                .OrderByDescending(i => i.CreatedAtUtc)
                .Take(50)
                .Select(i => new SearchInventoryRowVm
                {
                    Id = i.Id,
                    Title = i.Title,
                    Description = i.Description,
                    CategoryName = i.Category != null ? i.Category.Name : "Other",
                    CreatorName = i.Creator != null ? i.Creator.Name : "Unknown",
                    IsPublic = i.IsPublic,
                    CreatedAtUtc = i.CreatedAtUtc,
                })
                .ToListAsync();

            vm.Items = await _db.Items
                .AsNoTracking()
                .Where(it => allowedInventoryIds.Contains(it.InventoryId))
                .Include(it => it.Inventory)
                .Where(it => EF.Functions.ILike(it.CustomId, pattern) || EF.Functions.ILike(it.Title, pattern)

                )
                .OrderByDescending(it => it.CreatedAtUtc)
                .Take(100)
                .Select(it => new SearchItemRowVm
                {
                    InventoryId = it.InventoryId,
                    ItemId = it.Id,
                    InventoryTitle = it.Inventory != null ? it.Inventory.Title : "",
                    CustomId = it.CustomId,
                    Title = it.Title,
                    CreatedAtUtc = it.CreatedAtUtc,
                    UpdatedAtUtc = it.UpdatedAtUtc
                })
                .ToListAsync();

            return View(vm);
        }
    }
}
