using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Search;
using iLearning.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace iLearning.Web.Controllers
{
    [AllowAnonymous]
    [Route("search")]
    public class SearchController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;
        private readonly IStringLocalizer<SharedResource> T;

        public SearchController(AppDbContext db, CurrentUserService current, IStringLocalizer<SharedResource> t)
        {
            _db = db;
            _current = current;
            T = t;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index([FromQuery] string? q)
        {
            var vm = new SearchResultsVm
            {
                Q = (q ?? "").Trim()
            };

            vm.IsAuthenticated = _current.IsAuthenticated(User);
            vm.IsAdmin = _current.IsAdmin(User);

            if (string.IsNullOrWhiteSpace(vm.Q))
                return View(vm);

            if (vm.Q.Length > 200)
                vm.Q = vm.Q[..200];

            var isAuthenticated = vm.IsAuthenticated;
            var isAdmin = vm.IsAdmin;
            var userId = _current.GetUserId(User);

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
                .Where(i => allowedInventoryIds.Contains(i.Id))
                .Where(i => i.SearchVector.Matches(EF.Functions.PlainToTsQuery("simple", vm.Q)))
                .OrderByDescending(i => i.SearchVector.Rank(EF.Functions.PlainToTsQuery("simple", vm.Q)))
                .ThenByDescending(i => i.CreatedAtUtc)
                .Take(50)
                .Select(i => new SearchInventoryRowVm
                {
                    Id = i.Id,
                    Title = i.Title,
                    Description = i.Description,
                    CategoryName = i.Category != null ? i.Category.Name : T["Common.Other"],
                    CreatorName = i.Creator != null ? i.Creator.Name : T["Common.Unknown"],
                    IsPublic = i.IsPublic,
                    CreatedAtUtc = i.CreatedAtUtc,
                })
                .ToListAsync();

            vm.Items = await _db.Items
                .AsNoTracking()
                .Where(it => allowedInventoryIds.Contains(it.InventoryId))
                .Where(i => i.SearchVector.Matches(EF.Functions.PlainToTsQuery("simple", vm.Q)))
                .OrderByDescending(i => i.SearchVector.Rank(EF.Functions.PlainToTsQuery("simple", vm.Q)))
                .ThenByDescending (it => it.CreatedAtUtc)
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
