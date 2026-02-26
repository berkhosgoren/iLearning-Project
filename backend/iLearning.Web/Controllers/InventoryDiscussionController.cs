using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.Domain;
using iLearning.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iLearning.Web.Controllers
{
    [Authorize]
    [Route("inventories/{inventoryId:guid}/discussion")]
    public class InventoryDiscussionController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;

        public InventoryDiscussionController(AppDbContext db, CurrentUserService current)
        {
            _db = db;
            _current = current;
        }

        [ValidateAntiForgeryToken]
        [HttpPost("comments")]
        public async Task<IActionResult> AddComment(Guid inventoryId, [FromForm] string? body)
        {
            var canRead = await CanReadInventoryAsync(inventoryId);
            if (!canRead) return Forbid();

            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("login", "Auth");

            var text = (body ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                TempData["InventoryMessage"] = "Comment cannot be empty.";
                return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "discussion" });
            }

            if (text.Length > 1000)
                text = text[..1000];

            _db.InventoryComments.Add(new InventoryComment
            {
                InventoryId = inventoryId,
                UserId = userId.Value,
                Body = text,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Inventory Message"] = "Comment added.";
            return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "discussion" });
        }

        [ValidateAntiForgeryToken]
        [HttpPost("comments/{commentId:guid}/delete")]
        public async Task<IActionResult> DeleteComment(Guid inventoryId, Guid commentId)
        {
            var inv = await _db.Inventories
                .AsNoTracking()
                .Select(i => new { i.Id, i.CreatorId, i.IsPublic })
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv is null) return NotFound();

            var isAuthenticated = _current.IsAuthenticated(User);
            var userId = _current.GetUserId(User);
            var isAdmin = _current.IsAdmin(User);

            if (!isAuthenticated || !userId.HasValue)
                return RedirectToAction("login", "Auth");

            var canRead = inv.IsPublic || isAdmin || inv.CreatorId == userId.Value 
                || await _db.InventoryAccesses.AsNoTracking().AnyAsync(a => a.InventoryId == inventoryId && a.UserId == userId.Value);

            if (!canRead) return Forbid();

            var comment = await _db.InventoryComments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.InventoryId == inventoryId);

            if (comment is null) return NotFound();

            var canDelete = isAdmin || comment.UserId == userId.Value || inv.CreatorId == userId.Value;
            if (!canDelete) return Forbid();

            _db.InventoryComments.Remove(comment);
            await _db.SaveChangesAsync();

            TempData["InventoryMessage"] = "Comment Deleted.";
            return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "discussion" });
        }
        
        private async Task<bool> CanReadInventoryAsync(Guid inventoryId)
        {
            var inv = await _db.Inventories
                .AsNoTracking()
                .Select(i => new { i.Id, i.CreatorId, i.IsPublic })
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv is null) return false;

            if (inv.IsPublic) return true;

            if (!_current.IsAuthenticated(User)) return false;

            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return false;

            if (_current.IsAdmin(User)) return true;
            if (inv.CreatorId == userId.Value) return true;

            return await _db.InventoryAccesses
                .AsNoTracking()
                .AnyAsync(a => a.InventoryId == inventoryId && a.UserId == userId.Value);
        }
    }
}
