using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Account;
using iLearning.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;


namespace iLearning.Web.Controllers
{
    [Authorize]
    [Route("account")]
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;
        private readonly IStringLocalizer<SharedResource> T;

        public AccountController(AppDbContext db, CurrentUserService current, IStringLocalizer<SharedResource> t)
        {
            _db = db;
            _current = current;
            T = t;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            [FromQuery] string? oq,
            [FromQuery] string? os,
            [FromQuery] string? od,

            [FromQuery] string? aq,
            [FromQuery] string? @as,
            [FromQuery] string? ad,
            
            [FromQuery] string? rq,
            [FromQuery] string? rs,
            [FromQuery] string? rd)


        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            var isAdmin = _current.IsAdmin(User);

            var vm = new AccountIndexVm
            {
                IsAdmin = isAdmin,

                OwnedQuery = oq,
                OwnedSort = NormalizeSort(os),
                OwnedDir = NormalizeDir(od),

                AccessQuery = aq,
                AccessSort = NormalizeSort(@as),
                AccessDir = NormalizeDir(ad),

                ReadQuery = rq,
                ReadSort = NormalizeSort(rs),
                ReadDir = NormalizeDir(rd),
            };

            IQueryable<Models.Domain.Inventory> ownedQuery = _db.Inventories
                .AsNoTracking()
                .Include(i => i.Category);


            if (isAdmin)
            {
                ownedQuery = ownedQuery.Include(i => i.Creator);
            }
            else
            {
                ownedQuery = ownedQuery.Where(i => i.CreatorId == userId.Value);
            }

            ownedQuery = ApplyInventorySearch(ownedQuery, vm.OwnedQuery, isAdmin);
            ownedQuery = ApplySort(ownedQuery, vm.OwnedSort, vm.OwnedDir);

            vm.Owned = await ownedQuery
                .Take(200)
                .Select(i => new InventoryRowVm
                {
                    Id = i.Id,
                    Title = i.Title,
                    CategoryName = i.Category != null ? i.Category.Name : T["Common.Other"],
                    IsPublic = i.IsPublic,
                    CreatedAtUtc = i.CreatedAtUtc,
                    OwnerName = isAdmin ? (i.Creator != null ? i.Creator.Name : T["Common.Unknown"])
                    : ""
                })
                .ToListAsync();
            
            if (!isAdmin)
            {
                var accessBase = _db.InventoryAccesses
                .AsNoTracking()
                .Where(a => a.UserId == userId.Value && a.CanWrite)
                .Select(a => a.InventoryId);

                var accessQuery = _db.Inventories
                    .AsNoTracking()
                    .Include(i => i.Category)
                    .Include(i => i.Creator)
                    .Where(i => accessBase.Contains(i.Id) && i.CreatorId != userId.Value);

                accessQuery = ApplyInventorySearch(accessQuery, vm.AccessQuery, includeCreatorName: true);
                accessQuery = ApplySort(accessQuery, vm.AccessSort, vm.AccessDir);

                vm.Access = await accessQuery
                    .Take(200)
                    .Select(i => new AccessInventoryRowVm
                    {
                        Id = i.Id,
                        Title = i.Title,
                        CategoryName = i.Category != null ? i.Category.Name : T["Common.Other"],
                        IsPublic = i.IsPublic,
                        CreatedAtUtc = i.CreatedAtUtc,
                        OwnerName = i.Creator != null ? i.Creator.Name : T["Common.Unknown"]
                    })
                    .ToListAsync();

                var readOnlyIds = _db.InventoryAccesses
                    .AsNoTracking()
                    .Where(a => a.UserId == userId.Value && !a.CanWrite)
                    .Select(a => a.InventoryId);

                var readQuery = _db.Inventories
                    .AsNoTracking()
                    .Include(i => i.Category)
                    .Include(i => i.Creator)
                    .Where(i => readOnlyIds.Contains(i.Id) && i.CreatorId != userId.Value);

                readQuery = ApplyInventorySearch(readQuery, vm.ReadQuery, includeCreatorName: true);
                readQuery = ApplySort(readQuery, vm.ReadSort, vm.ReadDir);

                vm.ReadOnly = await readQuery
                    .Take(200)
                    .Select(i => new ReadOnlyInventoryRowVm
                    {
                        Id = i.Id,
                        Title = i.Title,
                        CategoryName = i.Category != null ? i.Category.Name : T["Common.Other"],
                        IsPublic = i.IsPublic,
                        CreatedAtUtc = i.CreatedAtUtc,
                        OwnerName = i.Creator != null ? i.Creator.Name : T["Common.Unknown"]
                    })
                    .ToListAsync();
            }      

            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("owned/delete")]
        public async Task<IActionResult> DeleteOwned([FromForm] Guid[] ids)
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            if (ids == null || ids.Length == 0)
                return RedirectToAction(nameof(Index));

            var ownedToDelete = await _db.Inventories
                .Where(i => i.CreatorId == userId.Value && ids.Contains(i.Id))
                .ToListAsync();

            if (ownedToDelete.Count == 0)
                return RedirectToAction(nameof(Index));

            _db.Inventories.RemoveRange(ownedToDelete);
            await _db.SaveChangesAsync();

            TempData["AccountMessage"] = T["Account.Messages.OwnedDeleted", ownedToDelete.Count].Value;
            return RedirectToAction(nameof(Index));
        }

        private static IQueryable<Models.Domain.Inventory> ApplyInventorySearch(
            IQueryable<Models.Domain.Inventory> query,
            string? rawQuery,
            bool includeCreatorName)
        {
            if (string.IsNullOrWhiteSpace(rawQuery))
                return query;

            var term = rawQuery.Trim();
            if (term.Length > 200)
                term = term[..200];

            var creatorPrefix = term + "%";

            if (includeCreatorName)
            {
                query = query.Where(i =>
                    i.SearchVector.Matches(term) ||
                    (i.Creator != null && EF.Functions.ILike(i.Creator.Name, creatorPrefix)));
            }
            else
            {
                query = query.Where(i => i.SearchVector.Matches(term));
            }

            return query;
        }

        private static string NormalizeSort(string? s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            return s switch
            {
                "title" => "title",
                "category" => "category",
                "public" => "public",
                "created" => "created",
                _ => "created"
            };
        }

        private static string NormalizeDir(string? d)
        {
            d = (d ?? "").Trim().ToLowerInvariant();
            return d == "asc" ? "asc" : "desc";
        }

        private static IQueryable<Models.Domain.Inventory> ApplySort(
            IQueryable<Models.Domain.Inventory> q,string sort, string dir)
        {
            var asc = dir == "asc";

            return sort switch
            {
                "title" => asc ? q.OrderBy(x => x.Title) : q.OrderByDescending(x => x.Title),
                "category" => asc ? q.OrderBy(x => x.Category.Name) : q.OrderByDescending(x => x.Category.Name),
                "public" => asc ? q.OrderBy(x => x.IsPublic) : q.OrderByDescending(X => X.IsPublic),
                _ => asc ? q.OrderBy(x => x.CreatedAtUtc) : q.OrderByDescending(x => x.CreatedAtUtc),
            };
        }
    }
}
