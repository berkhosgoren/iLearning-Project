using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Inventories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Services;

namespace iLearning.Web.Controllers
{
    [Route("inventories")]
    public class InventoriesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _currentUser;

        public InventoriesController(AppDbContext db, CurrentUserService currentUser)
        {
            _db = db; 
            _currentUser = currentUser;
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(Guid id, [FromQuery] string? tab)
        {
            var inv = await _db.Inventories
                .AsNoTracking()
                .Include(i => i.Category)
                .Include(i => i.Creator)
                .Include(i => i.InventoryTags).ThenInclude(it => it.Tag)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inv == null)
                return NotFound();

            var userId = _currentUser.GetUserId(User);
            var isAuthenticated = _currentUser.IsAuthenticated(User);
            var isAdmin = _currentUser.IsAdmin(User);

            var isOwner = isAuthenticated && userId.HasValue && userId.Value == inv.CreatorId;

            var canEdit = isAdmin || isOwner;

            bool hasExplicitWriteAccess = false;

            if (!canEdit && isAuthenticated && userId.HasValue)
            {
                hasExplicitWriteAccess = await _db.InventoryAccesses
                    .AsNoTracking()
                    .AnyAsync(a => a.InventoryId == inv.Id && a.UserId == userId.Value && a.CanWrite);
            }

            var canWrite = canEdit || (isAuthenticated && inv.IsPublic) || hasExplicitWriteAccess;

            var vm = new InventoryDetailsVm
            {
                Id = inv.Id,
                Title = inv.Title,
                Description = inv.Description,
                ImageUrl = inv.ImageUrl,
                CategoryName = inv.Category?.Name ?? "Other",
                IsPublic = inv.IsPublic,
                CreatorName = inv.Creator?.Name ?? "Unknown",
                CreatedAtUtc = inv.CreatedAtUtc,
                Tags = inv.InventoryTags.Select(x => x.Tag.Name).OrderBy(x => x).ToList(),
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "items" : tab.Trim().ToLowerInvariant(),
                CanEdit = canEdit,
                CanWrite = canWrite,
            };

            return View(vm);
        }
    
    }
}
