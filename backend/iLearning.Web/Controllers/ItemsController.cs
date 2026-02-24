using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Services;
using iLearning.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Models.ViewModels.Items;
using iLearning.Web.Models.Domain;

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

        [HttpGet("create")]
        public async Task <IActionResult> Create(Guid inventoryId)
        {
            var canWrite = await CanWriteInventoryAsync(inventoryId);
            if (!canWrite) return Forbid();

            var vm = new ItemUpsertVm
            {
                InventoryId = inventoryId,
            };

            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("create")]
        public async Task<IActionResult> Create(Guid inventoryId, ItemUpsertVm vm)
        {
            if (inventoryId != vm.InventoryId)
                return BadRequest();

            var canWrite = await CanWriteInventoryAsync(inventoryId);
            if (!canWrite) return Forbid();

            vm.CustomId = (vm.CustomId ?? "").Trim();
            vm.Title = (vm.Title ?? "").Trim();

            if (!ModelState.IsValid)
                return View(vm);

            var invExists = await _db.Inventories.AsNoTracking().AnyAsync(i => i.Id == inventoryId);
            if (!invExists) return NotFound();

            var customIdExists = await _db.Items
                .AsNoTracking()
                .AnyAsync(x => x.InventoryId ==  inventoryId && x.CustomId == vm.CustomId);

            if (customIdExists)
            {
                ModelState.AddModelError(nameof(vm.CustomId), "Custom ID already exists in this inventory");
                return View(vm); 
            }

            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("login", "Auth");

            var item = new Item
            {
                InventoryId = inventoryId,
                CustomId = vm.CustomId,
                Title = vm.Title,

                CreatedById = userId.Value,
                CreatedAtUtc = DateTime.UtcNow,

                Version = 1,

                String1 = string.IsNullOrWhiteSpace(vm.String1) ? null : vm.String1.Trim(),
                String2 = string.IsNullOrWhiteSpace(vm.String2) ? null : vm.String2.Trim(),
                String3 = string.IsNullOrWhiteSpace(vm.String3) ? null : vm.String3.Trim(),

                Text1 = string.IsNullOrWhiteSpace(vm.Text1) ? null : vm.Text1.Trim(),
                Text2 = string.IsNullOrWhiteSpace(vm.Text2) ? null : vm.Text2.Trim(),
                Text3 = string.IsNullOrWhiteSpace(vm.Text3) ? null : vm.Text3.Trim(),

                Number1 = vm.Number1,
                Number2 = vm.Number2,
                Number3 = vm.Number3,

                Bool1 = vm.Bool1,
                Bool2 = vm.Bool2,
                Bool3 = vm.Bool3,

                Link1 = string.IsNullOrWhiteSpace(vm.Link1) ? null : vm.Link1.Trim(),
                Link2 = string.IsNullOrWhiteSpace(vm.Link2) ? null : vm.Link2.Trim(),
                Link3 = string.IsNullOrWhiteSpace(vm.Link3) ? null : vm.Link3.Trim(),
            };

            _db.Items.Add(item);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(vm.CustomId), "Custom ID already exists in this inventory.");
                return View(vm);
            }

            TempData["InventoryMessage"] = "Item created.";
            return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "items" });
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

            var canWrite = canEdit || hasExplicitWrite;
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

        private async Task<bool> CanWriteInventoryAsync(Guid inventoryId)
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return false;

            if (_current.IsAdmin(User)) return true;

            var inv = await _db.Inventories
                .AsNoTracking()
                .Select(i => new { i.Id, i.CreatorId })
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return false;

            if (inv.CreatorId == userId.Value) return true;

            var hasExplicitWrite = await _db.InventoryAccesses
                .AsNoTracking()
                .AnyAsync(a => a.InventoryId == inventoryId && a.UserId == userId.Value && a.CanWrite);

            return hasExplicitWrite;
        }
    }
}
