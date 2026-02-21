using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Inventories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Services;
using iLearning.Web.Models.Domain;

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


        [Authorize]
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesAsync();

            var vm = new InventoryUpsertVm
            {
                CategoryId = await _db.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync()
            };

            return View(vm);  
        }

        [Authorize]
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryUpsertVm vm)
        {
            await LoadCategoriesAsync();

            if (!ModelState.IsValid)
                return View(vm);

            var userId = _currentUser.GetUserId(User);
            if (!userId.HasValue)
                return Forbid();

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId);
            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(vm.CategoryId), "Invalid category.");
                return View(vm);
            }

            var inv = new Inventory
            {
                Title = vm.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(vm.ImageUrl) ? null : vm.ImageUrl.Trim(),
                IsPublic = vm.IsPublic,
                CreatorId = userId.Value,
                CategoryId = vm.CategoryId,
                Version = 1,

                CustomString1Enabled = vm.CustomString1Enabled,
                CustomString1Name = NormalizeFieldName(vm.CustomString1Enabled, vm.CustomString1Name),
                CustomString2Enabled = vm.CustomString2Enabled,
                CustomString2Name = NormalizeFieldName(vm.CustomString2Enabled, vm.CustomString2Name),
                CustomString3Enabled = vm.CustomString3Enabled,
                CustomString3Name = NormalizeFieldName(vm.CustomString3Enabled, vm.CustomString3Name),

                CustomText1Enabled = vm.CustomText1Enabled,
                CustomText1Name = NormalizeFieldName(vm.CustomText1Enabled, vm.CustomText1Name),
                CustomText2Enabled = vm.CustomText2Enabled,
                CustomText2Name = NormalizeFieldName(vm.CustomText2Enabled, vm.CustomText2Name),
                CustomText3Enabled = vm.CustomText3Enabled,
                CustomText3Name = NormalizeFieldName(vm.CustomText3Enabled, vm.CustomText3Name),

                CustomNumber1Enabled = vm.CustomNumber1Enabled,
                CustomNumber1Name = NormalizeFieldName(vm.CustomNumber1Enabled, vm.CustomNumber1Name),
                CustomNumber2Enabled = vm.CustomNumber2Enabled,
                CustomNumber2Name = NormalizeFieldName(vm.CustomNumber2Enabled, vm.CustomNumber2Name),
                CustomNumber3Enabled = vm.CustomNumber3Enabled,
                CustomNumber3Name = NormalizeFieldName(vm.CustomNumber3Enabled, vm.CustomNumber3Name),

                CustomBool1Enabled = vm.CustomBool1Enabled,
                CustomBool1Name = NormalizeFieldName(vm.CustomBool1Enabled, vm.CustomBool1Name),
                CustomBool2Enabled = vm.CustomBool2Enabled,
                CustomBool2Name = NormalizeFieldName(vm.CustomBool2Enabled, vm.CustomBool2Name),
                CustomBool3Enabled = vm.CustomBool3Enabled,
                CustomBool3Name = NormalizeFieldName(vm.CustomBool3Enabled, vm.CustomBool3Name),

                CustomLink1Enabled = vm.CustomLink1Enabled,
                CustomLink1Name = NormalizeFieldName(vm.CustomLink1Enabled, vm.CustomLink1Name),
                CustomLink2Enabled = vm.CustomLink2Enabled,
                CustomLink2Name = NormalizeFieldName(vm.CustomLink2Enabled, vm.CustomLink2Name),
                CustomLink3Enabled = vm.CustomLink3Enabled,
                CustomLink3Name = NormalizeFieldName(vm.CustomLink3Enabled, vm.CustomLink3Name),
            };

            _db.Inventories.Add(inv);

            await UpsertInventoryTagsAsync(inv, vm.TagsCsv);

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = inv.Id });
        }


        [Authorize]
        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            await LoadCategoriesAsync();

            var inv = await _db.Inventories
                .Include(i => i.InventoryTags).ThenInclude(it => it.Tag)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inv is null)
                return NotFound();

            if (!await CanEditInventoryAsync(inv))
                return Forbid();

            var vm = new InventoryUpsertVm
            {
                Id = inv.Id,
                Title = inv.Title,
                Description = inv.Description,
                ImageUrl = inv.ImageUrl,
                CategoryId = inv.CategoryId,
                IsPublic = inv.IsPublic,
                Version = inv.Version,
                TagsCsv = string.Join(", ", inv.InventoryTags.Select(t => t.Tag.Name).OrderBy(x => x)),

                CustomString1Enabled = inv.CustomString1Enabled,
                CustomString1Name = inv.CustomString1Name,
                CustomString2Enabled = inv.CustomString2Enabled,
                CustomString2Name = inv.CustomString2Name,
                CustomString3Enabled = inv.CustomString3Enabled,
                CustomString3Name = inv.CustomString3Name,

                CustomText1Enabled = inv.CustomText1Enabled,
                CustomText1Name = inv.CustomText1Name,
                CustomText2Enabled = inv.CustomText2Enabled,
                CustomText2Name = inv.CustomText2Name,
                CustomText3Enabled = inv.CustomText3Enabled,
                CustomText3Name = inv.CustomText3Name,

                CustomNumber1Enabled = inv.CustomNumber1Enabled,
                CustomNumber1Name = inv.CustomNumber1Name,
                CustomNumber2Enabled = inv.CustomNumber2Enabled,
                CustomNumber2Name = inv.CustomNumber2Name,
                CustomNumber3Enabled = inv.CustomNumber3Enabled,
                CustomNumber3Name = inv.CustomNumber3Name,

                CustomBool1Enabled = inv.CustomBool1Enabled,
                CustomBool1Name = inv.CustomBool1Name,
                CustomBool2Enabled = inv.CustomBool2Enabled,
                CustomBool2Name = inv.CustomBool2Name,
                CustomBool3Enabled = inv.CustomBool3Enabled,
                CustomBool3Name = inv.CustomBool3Name,

                CustomLink1Enabled = inv.CustomLink1Enabled,
                CustomLink1Name = inv.CustomLink1Name,
                CustomLink2Enabled = inv.CustomLink2Enabled,
                CustomLink2Name = inv.CustomLink2Name,
                CustomLink3Enabled = inv.CustomLink3Enabled,
                CustomLink3Name = inv.CustomLink3Name,
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost("{id:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, InventoryUpsertVm vm)
        {
            await LoadCategoriesAsync();

            if (id != vm.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(vm);

            var inv = await _db.Inventories
                .Include(i => i.InventoryTags).ThenInclude(it => it.Tag)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inv is null)
                return NotFound();

            if (!await CanEditInventoryAsync(inv))
                return Forbid();

            inv.Version = vm.Version;

            inv.Title = vm.Title.Trim();
            inv.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            inv.ImageUrl = string.IsNullOrWhiteSpace(vm.ImageUrl) ? null : vm.ImageUrl.Trim();
            inv.IsPublic = vm.IsPublic;

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId);
            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(vm.CategoryId), "Invalid category.");
                return View(vm);
            }

            inv.CategoryId = vm.CategoryId;

            inv.CustomString1Enabled = vm.CustomString1Enabled;
            inv.CustomString1Name = NormalizeFieldName(vm.CustomString1Enabled, vm.CustomString1Name);
            inv.CustomString2Enabled = vm.CustomString2Enabled;
            inv.CustomString2Name = NormalizeFieldName(vm.CustomString2Enabled, vm.CustomString2Name);
            inv.CustomString3Enabled = vm.CustomString3Enabled;
            inv.CustomString3Name = NormalizeFieldName(vm.CustomString3Enabled, vm.CustomString3Name);

            inv.CustomText1Enabled = vm.CustomText1Enabled;
            inv.CustomText1Name = NormalizeFieldName(vm.CustomText1Enabled, vm.CustomText1Name);
            inv.CustomText2Enabled = vm.CustomText2Enabled;
            inv.CustomText2Name = NormalizeFieldName(vm.CustomText2Enabled, vm.CustomText2Name);
            inv.CustomText3Enabled = vm.CustomText3Enabled;
            inv.CustomText3Name = NormalizeFieldName(vm.CustomText3Enabled, vm.CustomText3Name);

            inv.CustomNumber1Enabled = vm.CustomNumber1Enabled;
            inv.CustomNumber1Name = NormalizeFieldName(vm.CustomNumber1Enabled, vm.CustomNumber1Name);
            inv.CustomNumber2Enabled = vm.CustomNumber2Enabled;
            inv.CustomNumber2Name = NormalizeFieldName(vm.CustomNumber2Enabled, vm.CustomNumber2Name);
            inv.CustomNumber3Enabled = vm.CustomNumber3Enabled;
            inv.CustomNumber3Name = NormalizeFieldName(vm.CustomNumber3Enabled, vm.CustomNumber3Name);

            inv.CustomBool1Enabled = vm.CustomBool1Enabled;
            inv.CustomBool1Name = NormalizeFieldName(vm.CustomBool1Enabled, vm.CustomBool1Name);
            inv.CustomBool2Enabled = vm.CustomBool2Enabled;
            inv.CustomBool2Name = NormalizeFieldName(vm.CustomBool2Enabled, vm.CustomBool2Name);
            inv.CustomBool3Enabled = vm.CustomBool3Enabled;
            inv.CustomBool3Name = NormalizeFieldName(vm.CustomBool3Enabled, vm.CustomBool3Name);

            inv.CustomLink1Enabled = vm.CustomLink1Enabled;
            inv.CustomLink1Name = NormalizeFieldName(vm.CustomLink1Enabled, vm.CustomLink1Name);
            inv.CustomLink2Enabled = vm.CustomLink2Enabled;
            inv.CustomLink2Name = NormalizeFieldName(vm.CustomLink2Enabled, vm.CustomLink2Name);
            inv.CustomLink3Enabled = vm.CustomLink3Enabled;
            inv.CustomLink3Name = NormalizeFieldName(vm.CustomLink3Enabled, vm.CustomLink3Name);

            await UpsertInventoryTagsAsync(inv, vm.TagsCsv);

            try
            {
                inv.Version += 1;
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) 
            {
                ModelState.AddModelError("", "This inventory was updated by someone else. Reload and try again.");
                return View(vm);
            }

            return RedirectToAction(nameof(Details), new { id = inv.Id });
        }


        private static string? NormalizeFieldName(bool enabled, string? name)
        {
            if (!enabled) return null;
            var trimmed = (name ?? "").Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        private async Task<bool> CanEditInventoryAsync(Inventory inv)
        {
            var userId = _currentUser.GetUserId(User);
            if (!userId.HasValue) return false;

            if (_currentUser.IsAdmin(User)) return true;
            return inv.CreatorId == userId.Value;
        }

        private async Task LoadCategoriesAsync()
        {
            var cats = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Categories = cats;
        }

        private async Task UpsertInventoryTagsAsync(Inventory inv, string? tagsCsv)
        {
            //simple vers with parse, normalize, ensure tag exist etc
            var tags = (tagsCsv ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Length > 60 ? t[..60] : t)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            inv.InventoryTags.Clear();

            if (tags.Count == 0)
                return;


            var existing = await _db.Tags
                .Where(t => tags.Contains(t.Name))
                .ToListAsync();

            foreach (var name in tags)
            {
                var tag = existing.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (tag is null)
                {
                    tag = new Tag { Name = name };
                    _db.Tags.Add(tag);
                    existing.Add(tag);
                }

                inv.InventoryTags.Add(new InventoryTag
                {
                    InventoryId = inv.Id,
                    TagId = tag.Id,
                    Tag = tag
                });
            }
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
