using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Services;
using iLearning.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Models.ViewModels.Items;
using iLearning.Web.Models.Domain;
using iLearning.Web.Models.ViewModels.Shared;


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

            var inv = await _db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return NotFound();

            var vm = new ItemUpsertVm
            {
                InventoryId = inventoryId,
                Fields = MapFieldConfig(inv)
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

            var inv = await _db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return NotFound();

            vm.Fields = MapFieldConfig(inv);

            vm.CustomId = (vm.CustomId ?? "").Trim();
            vm.Title = (vm.Title ?? "").Trim();
            ApplyFieldEnforcement(vm, inv);

            if (!ModelState.IsValid)
                return View(vm);

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

        [AllowAnonymous]
        [HttpGet("{itemId:guid}")]
        public async Task<IActionResult> Details(Guid inventoryId, Guid itemId)
        {
            var inv = await _db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return NotFound();

            var isAuthenticated = _current.IsAuthenticated(User);
            var userId = _current.GetUserId(User);
            var isAdmin = _current.IsAdmin(User);

            var isOwner = isAuthenticated && userId.HasValue && inv.CreatorId == userId.Value;
            var canEditInventory = isAdmin || isOwner;

            bool hasAnyAccessRow = false;
            bool hasExplicitWrite = false;

            if (!canEditInventory && isAuthenticated && userId.HasValue)
            {
                var access = await _db.InventoryAccesses
                    .AsNoTracking()
                    .Where(a => a.InventoryId == inventoryId && a.UserId == userId.Value)
                    .Select(a => new { a.CanWrite })
                    .FirstOrDefaultAsync();

                hasAnyAccessRow = access != null;
                hasExplicitWrite = access?.CanWrite == true;
            }

            var canRead = inv.IsPublic || (isAuthenticated && (canEditInventory || hasAnyAccessRow));

            if (!canRead) return Forbid();

            var canWrite = canEditInventory || (isAuthenticated && hasExplicitWrite);

            var vm = await _db.Items
                .AsNoTracking()
                .Where(X => X.InventoryId == inventoryId && X.Id == itemId)
                .Select(x => new ItemDetailsVm
                {
                    InventoryId = inventoryId,
                    ItemId = x.Id,

                    InventoryTitle = inv.Title,

                    CustomId = x.CustomId,
                    Title = x.Title,

                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc,

                    CreatedByName = x.CreatedBy != null ? x.CreatedBy.Name : "Unknown",
                    UpdatedByName = x.UpdatedBy != null ? x.UpdatedBy.Name : null,

                    LikesCount = x.Likes.Count,
                    CommentsCount = x.Comments.Count,

                    CanWrite = canWrite,
                    IsAuthenticated = isAuthenticated, 

                    Fields = MapFieldConfig(inv),

                    String1 = x.String1,
                    String2 = x.String2,
                    String3 = x.String3,

                    Text1 = x.Text1,
                    Text2 = x.Text2,
                    Text3 = x.Text3,

                    Number1 = x.Number1,
                    Number2 = x.Number2,
                    Number3 = x.Number3,

                    Bool1 = x.Bool1,
                    Bool2 = x.Bool2,
                    Bool3 = x.Bool3,

                    Link1 = x.Link1,
                    Link2 = x.Link2,
                    Link3 = x.Link3
                })
                .FirstOrDefaultAsync();

            if (vm == null) return NotFound();

            if (isAuthenticated && userId.HasValue)
            {
                vm.IsLikedByMe = await _db.ItemLikes
                    .AsNoTracking()
                    .AnyAsync(l => l.ItemId == itemId && l.UserId == userId.Value);
            }

            vm.Comments = await _db.ItemComments
                .AsNoTracking()
                .Where(c => c.ItemId == itemId)
                .OrderByDescending(c => c.CreatedAtUtc)
                .Take(200)
                .Select(c => new ItemCommentRowVm
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = c.User != null ? c.User.Name : "Unknown",
                    Body = c.Body,
                    CreatedAtUtc = c.CreatedAtUtc,
                    CanDelete = isAdmin || (isAuthenticated && userId.HasValue && (c.UserId == userId.Value ||inv.CreatorId == userId.Value))
                })
                .ToListAsync();

            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("{itemId:guid}/toggle-like")]
        public async Task<IActionResult> ToggleLike(Guid inventoryId, Guid itemId)
        {
            var canRead = await CanReadInventoryAsync(inventoryId);
            if (!canRead) return Forbid();

            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("login", "Auth");

            var itemExists = await _db.Items
                .AsNoTracking()
                .AnyAsync(i => i.InventoryId == inventoryId && i.Id == itemId);

            if (!itemExists) return NotFound();

            var existing = await _db.ItemLikes
                .FirstOrDefaultAsync(l => l.ItemId == itemId && l.UserId == userId.Value);

            if (existing != null)
            {
                _db.ItemLikes.Remove(existing);
                TempData["InventoryMessage"] = "Like Removed.";
            }
            else
            {
                _db.ItemLikes.Add(new ItemLike
                {
                    ItemId = itemId,
                    UserId = userId.Value,
                    CreatedAtUtc = DateTime.UtcNow
                });

                TempData["InventoryMessage"] = "Liked.";
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { inventoryId, itemId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost("{itemId:guid}/comments")]
        public async Task<IActionResult> AddComment(Guid inventoryId, Guid itemId, [FromForm] string? body)
        {
            var canRead = await CanReadInventoryAsync(inventoryId);
            if (!canRead) return Forbid();

            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("login", "Auth");

            var text = (body ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                TempData["InventoryMessage"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Details), new { inventoryId, itemId });
            }

            if (text.Length > 1000)
                text = text[..1000];

            var itemExists = await _db.Items
                .AsNoTracking()
                .AnyAsync(i => i.InventoryId == inventoryId && i.Id == itemId);

            if (!itemExists) return NotFound();

            _db.ItemComments.Add(new ItemComment
            {
                ItemId = itemId,
                UserId = userId.Value,
                Body = text,
                CreatedAtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["InventoryMessage"] = "Comment added.";
            return RedirectToAction(nameof(Details), new { inventoryId, itemId });
        }

        [ValidateAntiForgeryToken]
        [HttpPost("{itemId:guid}/comments/{commentId:guid}/delete")]
        public async Task<IActionResult> DeleteComment(Guid inventoryId, Guid itemId, Guid commentId)
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

            var comment = await _db.ItemComments
                .FirstOrDefaultAsync(c => c.Id == commentId && c.ItemId == itemId);

            if (comment == null) return NotFound();

            var canDelete = isAdmin || comment.UserId == userId.Value || inv.CreatorId == userId.Value;
            if (!canDelete) return Forbid();

            _db.ItemComments.Remove(comment);
            await _db.SaveChangesAsync();

            TempData["InventoryMessage"] = "Comment deleted.";
            return RedirectToAction(nameof(Details), new { inventoryId, itemId });
        }


        [HttpGet("{itemId:guid}/edit")]
        public async Task<IActionResult> Edit(Guid inventoryId, Guid itemId)
        {
            var canWrite = await CanWriteInventoryAsync(inventoryId);
            if (!canWrite) return Forbid();

            var inv = await _db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return NotFound();

            var item = await _db.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InventoryId == inventoryId && x.Id == itemId);

            if (item == null) return NotFound();

            var vm = new ItemUpsertVm
            {
                InventoryId = inventoryId,
                ItemId = itemId,
                Version = item.Version,

                CustomId = item.CustomId,
                Title = item.Title,

                String1 = item.String1,
                String2 = item.String2,
                String3 = item.String3,

                Text1 = item.Text1,
                Text2 = item.Text2,
                Text3 = item.Text3,

                Number1 = item.Number1,
                Number2 = item.Number2,
                Number3 = item.Number3,

                Bool1 = item.Bool1,
                Bool2 = item.Bool2,
                Bool3 = item.Bool3,

                Link1 = item.Link1,
                Link2 = item.Link2,
                Link3 = item.Link3,
            };

            vm.Fields = MapFieldConfig(inv);

            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("{itemId:guid}/edit")]
        public async Task<IActionResult> Edit(Guid inventoryId, Guid itemId, ItemUpsertVm vm)
        {
            if (inventoryId != vm.InventoryId || itemId != vm.ItemId) 
                return BadRequest();

            var canWrite = await CanWriteInventoryAsync(inventoryId);
            if (!canWrite) return Forbid();

            var inv = await _db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return NotFound();

            vm.Fields = MapFieldConfig(inv);

            vm.CustomId = (vm.CustomId ?? "").Trim();
            vm.Title = (vm.Title ?? "").Trim();

            if (!ModelState.IsValid)
                return View(vm);

            var item = await _db.Items
                .FirstOrDefaultAsync(x => x.InventoryId == inventoryId && x.Id == itemId);

            if (item == null) return NotFound();

            if (vm.Version != item.Version)
            {
                ModelState.AddModelError("", "This item was updated by someone else, reload and try again.");
                vm.Version = item.Version;
                return View(vm);
            }

            var customIdTaken = await _db.Items
                .AsNoTracking()
                .AnyAsync(x => x.InventoryId == inventoryId && x.CustomId == vm.CustomId && x.Id != itemId);

            if (customIdTaken)
            {
                ModelState.AddModelError(nameof(vm.CustomId), "Custom ID already exists in this inventory.");
                return View(vm);
            }

            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return RedirectToAction("login", "Auth");

            item.CustomId = vm.CustomId;
            item.Title = vm.Title;

            item.String1 = string.IsNullOrWhiteSpace(vm.String1) ? null : vm.String1.Trim();
            item.String2 = string.IsNullOrWhiteSpace(vm.String2) ? null : vm.String2.Trim();
            item.String3 = string.IsNullOrWhiteSpace(vm.String3) ? null : vm.String3.Trim();

            item.Text1 = string.IsNullOrWhiteSpace(vm.Text1) ? null : vm.Text1.Trim();
            item.Text2 = string.IsNullOrWhiteSpace(vm.Text2) ? null : vm.Text2.Trim();
            item.Text3 = string.IsNullOrWhiteSpace(vm.Text3) ? null : vm.Text3.Trim();

            item.Number1 = vm.Number1;
            item.Number2 = vm.Number2;
            item.Number3 = vm.Number3;

            item.Bool1 = vm.Bool1;
            item.Bool2 = vm.Bool2;
            item.Bool3 = vm.Bool3;

            item.Link1 = string.IsNullOrWhiteSpace(vm.Link1) ? null : vm.Link1.Trim();
            item.Link2 = string.IsNullOrWhiteSpace(vm.Link2) ? null : vm.Link2.Trim();
            item.Link3 = string.IsNullOrWhiteSpace(vm.Link3) ? null : vm.Link3.Trim();

            item.UpdatedById = userId.Value;
            item.UpdatedAtUtc = DateTime.UtcNow;

            item.Version += 1;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Could not save changes. Try again.");
                return View(vm);
            }

            TempData["InventoryMessage"] = "Item updated.";
            return RedirectToAction(nameof(Details), new { inventoryId, itemId });

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

        private async Task<bool> CanReadInventoryAsync(Guid inventoryId)
        {
            var inv = await _db.Inventories
                .AsNoTracking()
                .Select(i => new { i.Id, i.CreatorId, i.IsPublic })
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inv == null) return false;

            if (inv.IsPublic) return true;

            var isAuthenticated = _current.IsAuthenticated(User);
            if (!isAuthenticated) return false;

            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return false;

            if (_current.IsAdmin(User)) return true;
            if (inv.CreatorId == userId.Value) return true;

            var hasAnyAccess = await _db.InventoryAccesses
                .AsNoTracking()
                .AnyAsync(a => a.InventoryId == inventoryId && a.UserId == userId.Value);

            return hasAnyAccess;
        }

        private static InventoryFieldConfigVm MapFieldConfig(Inventory inv)
        {
            return new InventoryFieldConfigVm
            {
                String1Enabled = inv.CustomString1Enabled,
                String1Name = inv.CustomString1Name,
                String2Enabled = inv.CustomString2Enabled,
                String2Name = inv.CustomString2Name,
                String3Enabled = inv.CustomString3Enabled,
                String3Name = inv.CustomString3Name,

                Text1Enabled = inv.CustomText1Enabled,
                Text1Name = inv.CustomText1Name,
                Text2Enabled = inv.CustomText2Enabled,
                Text2Name = inv.CustomText2Name,
                Text3Enabled = inv.CustomText3Enabled,
                Text3Name = inv.CustomText3Name,

                Number1Enabled = inv.CustomNumber1Enabled,
                Number1Name = inv.CustomNumber1Name,
                Number2Enabled = inv.CustomNumber2Enabled,
                Number2Name = inv.CustomNumber2Name,
                Number3Enabled = inv.CustomNumber3Enabled,
                Number3Name = inv.CustomNumber3Name,

                Bool1Enabled = inv.CustomBool1Enabled,
                Bool1Name = inv.CustomBool1Name,
                Bool2Enabled = inv.CustomBool2Enabled,
                Bool2Name = inv.CustomBool2Name,
                Bool3Enabled = inv.CustomBool3Enabled,
                Bool3Name = inv.CustomBool3Name,

                Link1Enabled = inv.CustomLink1Enabled,
                Link1Name = inv.CustomLink1Name,
                Link2Enabled = inv.CustomLink2Enabled,
                Link2Name = inv.CustomLink2Name,
                Link3Enabled = inv.CustomLink3Enabled,
                Link3Name = inv.CustomLink3Name
            };
        }

        private static void ApplyFieldEnforcement(ItemUpsertVm vm, Inventory inv)
        {
            if (!inv.CustomString1Enabled) vm.String1 = null;
            if (!inv.CustomString2Enabled) vm.String2 = null;
            if (!inv.CustomString3Enabled) vm.String3 = null;

            if (!inv.CustomText1Enabled) vm.Text1 = null;
            if (!inv.CustomText2Enabled) vm.Text2 = null;
            if (!inv.CustomText3Enabled) vm.Text3 = null;

            if (!inv.CustomNumber1Enabled) vm.Number1 = null;
            if (!inv.CustomNumber2Enabled) vm.Number2 = null;
            if (!inv.CustomNumber3Enabled) vm.Number3 = null;

            if (!inv.CustomBool1Enabled) vm.Bool1 = null;
            if (!inv.CustomBool2Enabled) vm.Bool2 = null;
            if (!inv.CustomBool3Enabled) vm.Bool3 = null;

            if (!inv.CustomLink1Enabled) vm.Link1 = null;
            if (!inv.CustomLink2Enabled) vm.Link2 = null;
            if (!inv.CustomLink3Enabled) vm.Link3 = null;
        }
    }
}
