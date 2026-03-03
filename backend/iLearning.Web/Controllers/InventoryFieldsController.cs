using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Inventories;
using iLearning.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace iLearning.Web.Controllers
{
    [Authorize]
    [Route("inventories/{inventoryId:guid}/fields")]
    public class InventoryFieldsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;
        private readonly IStringLocalizer<SharedResource> T;

        public InventoryFieldsController(AppDbContext db, CurrentUserService current, IStringLocalizer<SharedResource> t)
        {
            _db = db;
            _current = current;
            T = t;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(Guid inventoryId)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == inventoryId);
            if (inv == null) return NotFound();

            if (!CanEdit(inv)) return Forbid();

            var vm = new InventoryFieldsPageVm
            {
                InventoryId = inv.Id,
                InventoryTitle = inv.Title,
                IsPublic = inv.IsPublic,
                CanEdit = true,
                Version = inv.Version,

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
                CustomLink3Name = inv.CustomLink3Name

            };

            return View(vm);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("")]
        public async Task<IActionResult> Save(Guid inventoryId, InventoryFieldsPageVm vm)
        {
            if (inventoryId != vm.InventoryId) return BadRequest();

            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == inventoryId);
            if (inv == null) return NotFound();

            if (!CanEdit(inv)) return Forbid();

            if (vm.Version != inv.Version)
            {
                ModelState.AddModelError("", T["InventoryFields.Errors.ConcurrencyReload"]);
                vm.InventoryTitle = inv.Title;
                vm.IsPublic = inv.IsPublic;
                return View("Index", vm);
            }

            inv.CustomString1Enabled = vm.CustomString1Enabled;
            inv.CustomString1Name = Normalize(vm.CustomString1Enabled, vm.CustomString1Name);

            inv.CustomString2Enabled = vm.CustomString2Enabled;
            inv.CustomString2Name = Normalize(vm.CustomString2Enabled, vm.CustomString2Name);

            inv.CustomString3Enabled = vm.CustomString3Enabled;
            inv.CustomString3Name = Normalize(vm.CustomString3Enabled, vm.CustomString3Name);

            inv.CustomText1Enabled = vm.CustomText1Enabled;
            inv.CustomText1Name = Normalize(vm.CustomText1Enabled, vm.CustomText1Name);

            inv.CustomText2Enabled = vm.CustomText2Enabled;
            inv.CustomText2Name = Normalize(vm.CustomText2Enabled, vm.CustomText2Name);

            inv.CustomText3Enabled = vm.CustomText3Enabled;
            inv.CustomText3Name = Normalize(vm.CustomText3Enabled, vm.CustomText3Name);

            inv.CustomNumber1Enabled = vm.CustomNumber1Enabled;
            inv.CustomNumber1Name = Normalize(vm.CustomNumber1Enabled, vm.CustomNumber1Name);

            inv.CustomNumber2Enabled = vm.CustomNumber2Enabled;
            inv.CustomNumber2Name = Normalize(vm.CustomNumber2Enabled, vm.CustomNumber2Name);

            inv.CustomNumber3Enabled = vm.CustomNumber3Enabled;
            inv.CustomNumber3Name = Normalize(vm.CustomNumber3Enabled, vm.CustomNumber3Name);

            inv.CustomBool1Enabled = vm.CustomBool1Enabled;
            inv.CustomBool1Name = Normalize(vm.CustomBool1Enabled, vm.CustomBool1Name);

            inv.CustomBool2Enabled = vm.CustomBool2Enabled;
            inv.CustomBool2Name = Normalize(vm.CustomBool2Enabled, vm.CustomBool2Name);

            inv.CustomBool3Enabled = vm.CustomBool3Enabled;
            inv.CustomBool3Name = Normalize(vm.CustomBool3Enabled, vm.CustomBool3Name);

            inv.CustomLink1Enabled = vm.CustomLink1Enabled;
            inv.CustomLink1Name = Normalize(vm.CustomLink1Enabled, vm.CustomLink1Name);

            inv.CustomLink2Enabled = vm.CustomLink2Enabled;
            inv.CustomLink2Name = Normalize(vm.CustomLink2Enabled, vm.CustomLink2Name);

            inv.CustomLink3Enabled = vm.CustomLink3Enabled;
            inv.CustomLink3Name = Normalize(vm.CustomLink3Enabled, vm.CustomLink3Name);

            inv.Version += 1;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", T["InventoryFields.Errors.ConcurrencyReload"]);
                vm.InventoryTitle = inv.Title;
                vm.IsPublic = inv.IsPublic;
                return View("Index", vm);
            }

            TempData["InventoryMessage"] = T["InventoryFields.Messages.FieldsUpdated"];
            return RedirectToAction("Details", "Inventories", new { id = inventoryId, tab = "items" });
        }

        private bool CanEdit(Models.Domain.Inventory inv)
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue) return false;

            if (_current.IsAdmin(User)) return true;
            return inv.CreatorId == userId.Value;
        }

        private static string? Normalize(bool enabled, string? name)
        {
            if (!enabled) return null;
            var t = (name ?? "").Trim();
            if (t.Length > 100) t = t[..100];
            return string.IsNullOrWhiteSpace(t) ? null : t;
        }
    }
}
