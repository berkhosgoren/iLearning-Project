using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;


namespace iLearning.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("admin/users")]
    public class AdminUsersController : Controller
    {
        private readonly AppDbContext _db;

        public AdminUsersController(AppDbContext db)
        {
            _db = db;  
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(
            [FromQuery] string? q,
            [FromQuery] string? s,
            [FromQuery] string? d)
        {
            var vm = new AdminUsersIndexVm
            {
                Q = q,
                Sort = NormalizeSort(s),
                Dir = NormalizeDir(d),
                Message = TempData["AdminUsersMessage"] as string
            };

            var query = _db.Users.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(vm.Q))
            {
                var search = vm.Q.Trim().ToLowerInvariant();
                query = query.Where(u => 
                    u.Name.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
            }

            query = ApplySort(query, vm.Sort, vm.Dir);

            var usersPage = await query
                .Take(300)
                .Select(u => new
                {
                    u.Id,
                    u.Name,
                    u.Email,
                    u.IsBlocked,
                    u.CreatedAtUtc
                })
                .ToListAsync();

            var userIds = usersPage.Select(x => x.Id).ToList();

            var adminRoleId = await _db.Roles
                .AsNoTracking()
                .Where(r => r.Name == "Admin")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var adminUserIds = adminRoleId == 0 ? new HashSet<Guid>()
                : (await _db.UserRoles
                    .AsNoTracking()
                    .Where(ur => ur.RoleId == adminRoleId && userIds.Contains(ur.UserId))
                    .Select(ur => ur.UserId)
                    .ToListAsync()).ToHashSet();

            var ownedCounts = await _db.Inventories
                .AsNoTracking()
                .Where(i => userIds.Contains(i.CreatorId))
                .GroupBy(i => i.CreatorId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            vm.Users = usersPage.Select(u => new AdminUserRowVm
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                IsBlocked = u.IsBlocked,
                CreatedAtUtc = u.CreatedAtUtc,
                IsAdmin = adminUserIds.Contains(u.Id),
                OwnedInventoriesCount = ownedCounts.TryGetValue(u.Id, out var c) ? c : 0
            }).ToList();

            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("block")]
        public async Task<IActionResult> Block([FromForm] Guid[] ids)
            => await SetBlocked(ids, true);

        [ValidateAntiForgeryToken]
        [HttpPost("unlock")]
        public async Task<IActionResult> Unblock([FromForm] Guid[] ids)
            => await SetBlocked(ids, false);

        [ValidateAntiForgeryToken]
        [HttpPost("delete")]
        public async Task<IActionResult> Delete([FromForm] Guid[] ids)
        {
            if (ids == null || ids.Length == 0)
                return RedirectToAction(nameof(Index));

            var users = await _db.Users
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            if (users.Count == 0)
                return RedirectToAction(nameof(Index));

            _db.Users.RemoveRange(users);
            await _db.SaveChangesAsync();

            TempData["AdminUsersMessage"] = $"Deleted {users.Count} users(s).";
            return RedirectToAction(nameof(Index));
        }

        [ValidateAntiForgeryToken]
        [HttpPost("toggle-admin")]
        public async Task<IActionResult> ToggleAdmin([FromForm] Guid[] ids)
        {
            if (ids == null || ids.Length == 0)
                return RedirectToAction(nameof(Index));

            var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            if (adminRole is null)
            {
                TempData["AdminUsersMessage"] = "Admin role not found.";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _db.UserRoles
                .Where(ur => ur.RoleId == adminRole.Id && ids.Contains(ur.UserId))
                .ToListAsync();

            var existingSet = existing.Select(x => x.UserId).ToHashSet();

            var toRemove = existing;
            if (toRemove.Count > 0)
                _db.UserRoles.RemoveRange(toRemove);

            var toAddIds = ids.Where(id => !existingSet.Contains(id)).Distinct().ToList();
            foreach (var id in toAddIds)
                _db.UserRoles.Add(new iLearning.Web.Models.Domain.UserRole { UserId = id, RoleId = adminRole.Id });

            await _db.SaveChangesAsync();

            TempData["AdminUsersMessage"] = $"Admin role updated. Added: {toAddIds.Count}, removed {toRemove.Count}";
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> SetBlocked(Guid[] ids, bool blocked)
        {
            if (ids == null || ids.Length == 0)
                return RedirectToAction(nameof(Index));

            var users = await _db.Users
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            if (users.Count == 0)
                return RedirectToAction(nameof(Index));

            foreach (var u in users)
                u.IsBlocked = blocked;

            await _db.SaveChangesAsync();

            TempData["AdminUsersMessage"] = blocked
                ? $"Blocked {users.Count} user(s)." : $"Unblocked {users.Count} user(s).";

            return RedirectToAction(nameof(Index));
        }

        private static string NormalizeSort(string? s)
        {
            s = (s ?? "").Trim().ToLowerInvariant();
            return s switch
            {
                "name" => "name",
                "email" => "email",
                "blocked" => "blocked",
                "created" => "created",
                _ => "created"
            };
        }

        private static string NormalizeDir(string? d)
        {
            d = (d ?? "").Trim().ToLowerInvariant();
            return d == "asc" ? "asc" : "desc";
        }

        private static IQueryable<iLearning.Web.Models.Domain.AppUser> ApplySort(
            IQueryable<iLearning.Web.Models.Domain.AppUser> q, string sort, string dir)
        {
            var asc = dir == "asc";

            return sort switch
            {
                "name" => asc ? q.OrderBy(x => x.Name) : q.OrderByDescending(x => x.Name),
                "email" => asc ? q.OrderBy(x => x.Email) : q.OrderByDescending(x => x.Email),
                "blocked" => asc ? q.OrderBy(x => x.IsBlocked) : q.OrderByDescending(x => x.IsBlocked),
                _ => asc ? q.OrderBy(x => x.CreatedAtUtc) : q.OrderByDescending(x => x.CreatedAtUtc)
            };
        }
    }
}
