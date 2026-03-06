using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Inventories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Services;
using Microsoft.Extensions.Localization;
using iLearning.Web.Models.Domain;

namespace iLearning.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("autosave")]
    public class AutoSaveController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;
        private IStringLocalizer<SharedResource> T;

        public AutoSaveController(AppDbContext db, CurrentUserService current, IStringLocalizer<SharedResource> t)
        {
            _db = db;
            _current = current;
            T = t;
        }

        [ValidateAntiForgeryToken]
        [HttpPost("inventory-settings/{id:guid}")]
        public async Task<IActionResult> InventorySettings(Guid id, InventoryUpsertVm vm)
        {
           if (id != vm.Id) 
                return BadRequest();

           var inv = await _db.Inventories
                .Include(i => i.InventoryTags)
                .ThenInclude(it => it.Tag)
                .FirstOrDefaultAsync(i => i.Id == id);
           
            if (inv == null)
                return NotFound();

            if (!CanEdit(inv))
                return Forbid();

            if (vm.Version != inv.Version)
            {
                return StatusCode(409, new
                {
                    message = T["Inv.Err.Concurrency"].Value,
                    serverVersion = inv.Version
                });
            }

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId);
            if (!categoryExists)
            {
                return BadRequest(new
                {
                    message = T["Inv.Err.InvalidCategory"].Value
                });
            }

            inv.Title = (vm.Title ?? string.Empty).Trim();
            inv.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            inv.ImageUrl = string.IsNullOrWhiteSpace(vm.ImageUrl) ? null : vm.ImageUrl.Trim();
            inv.IsPublic = vm.IsPublic;
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

            inv.Version += 1;
            await _db.SaveChangesAsync();

            return Json(new
            {
                ok = true,
                newVersion = inv.Version,
                savedAtUtc = DateTime.UtcNow,
            });
        }

        [ValidateAntiForgeryToken]
        [HttpPost("inventory-fields/{id:guid}")]
        public async Task<IActionResult> InventoryFields(Guid id, InventoryFieldsPageVm vm)
        {
            if (id != vm.InventoryId)
                return BadRequest();

            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == id);
            if (inv == null) 
                return NotFound();

            if (!CanEdit(inv))
                return Forbid();

            if (vm.Version != inv.Version)
            {
                return StatusCode(409, new
                {
                    message = T["Inv.Err.Concurrency"].Value,
                    serverVersion = inv.Version
                });
            }

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

            inv.Version += 1;
            await _db.SaveChangesAsync();

            return Json(new
            {
                ok = true,
                newVersion = inv.Version,
                savedAtUtc = DateTime.UtcNow
            });
        }

        private bool CanEdit(Inventory inv)
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return false;

            if (_current.IsAdmin(User)) return true;
            return inv.CreatorId == userId.Value;
        }

        private static string? NormalizeFieldName(bool enabled, string? name)
        {
            if (!enabled) return null;

            var trimmed = (name ?? "").Trim();
            if (trimmed.Length > 100)
                trimmed = trimmed[..100];

            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        private async Task UpsertInventoryTagsAsync(Inventory inv, string? tagsCsv)
        {
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
                if (tag == null)
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

    }
}
