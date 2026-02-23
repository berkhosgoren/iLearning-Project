using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Services;
using iLearning.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iLearning.Web.Controllers
{
    [Authorize]
    [Route("inventories/{inventoryId:guid}/items")]
    public class ItemsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;

        public ItemsController(AppDbContext db, CurrentUserService current)
        {
            _db = db;
            _current = current;
        }

        [ValidateAntiForgeryToken]
        [HttpPost("bulk-delete")]
        public async Task<IActionResult> BulkDelete(Guid inventoryId, [FromForm] Guid[] ids)
        {
            if (ids == null || ids.Length == 0)
                return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "items" });

            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("Login", "Auth");

            var isAdmin = _current.IsAdmin(User);

            var inv = await _db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return NotFound();

            var isOwner = inv.CreatorId == userId.Value;
            var canEdit = isAdmin || isOwner;

            var hasExplicitWrite = false;
            if (!canEdit)
            {
                hasExplicitWrite = await _db.InventoryAccesses
                    .AsNoTracking()
                    .AnyAsync(a => a.InventoryId == inventoryId && a.UserId == userId.Value && a.CanWrite);
            }

            var canWrite = canEdit || hasExplicitWrite || (inv.IsPublic && _current.IsAuthenticated(User));
            if (!canWrite) return Forbid();

            var itemsToDelete = await _db.Items
                .Where(x => x.InventoryId == inventoryId && ids.Contains(x.Id))
                .ToListAsync();

            if (itemsToDelete.Count == 0)
                return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "items" });

            _db.Items.RemoveRange(itemsToDelete);
            await _db.SaveChangesAsync();

            TempData["InventoryMessage"] = $"Deleted {itemsToDelete.Count} item(s).";
            return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "items" });
        }
    }
}
