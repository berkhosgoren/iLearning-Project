using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Inventories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iLearning.Web.Services;
using iLearning.Web.Models.Domain;
using iLearning.Web.Models.ViewModels.Items;
using Microsoft.Extensions.Localization;
using iLearning.Web.Services.Images;

namespace iLearning.Web.Controllers
{
    [Route("inventories")]
    public class InventoriesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _currentUser;
        private readonly IMarkdownService _markdown;
        private readonly IStringLocalizer<SharedResource> T;
        private readonly IInventoryImageService _inventoryImageService;

        private const long MaxImageFileBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp"
        };

        public InventoriesController(AppDbContext db, CurrentUserService currentUser, IMarkdownService markdown, IStringLocalizer<SharedResource> t, IInventoryImageService inventoryImageService)
        {
            _db = db; 
            _currentUser = currentUser;
            _markdown = markdown;
            T = t;
            _inventoryImageService = inventoryImageService;
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

            vm.Title = (vm.Title ?? "").Trim();
            vm.ImageUrl = NormalizeImageUrl(vm.ImageUrl);

            if (string.IsNullOrWhiteSpace(vm.Title))
            {
                ModelState.AddModelError(nameof(vm.Title), T["Common.Required", T["Inv.Title"]].Value);
            }

            if (!ValidateImageFile(vm.ImageFile))
            {
                return View(vm);
            }

            if (!ModelState.IsValid)
                return View(vm);

            var userId = _currentUser.GetUserId(User);
            if (!userId.HasValue)
                return Forbid();

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId);
            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(vm.CategoryId), T["Inv.Err.InvalidCategory"]);
                return View(vm);
            }

            var imageUrl = await ResolveInventoryImageUrlAsync(vm);
            if (!ModelState.IsValid)
                return View(vm);

            var inv = new Inventory
            {
                Title = vm.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                ImageUrl = imageUrl,
                IsPublic = vm.IsPublic,
                CreatorId = userId.Value,
                CategoryId = vm.CategoryId,
                Version = 1,

                ItemCustomIdEnabled = false,
                ItemCustomIdPrefix = null,
                ItemCustomIdDigits = 4,
                ItemCustomIdNextNumber = 1,

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
            return RedirectToAction(nameof(Details), new { id, tab = "settings" });
        }

        [Authorize]
        [HttpPost("{id:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, InventoryUpsertVm vm)
        {
            await LoadCategoriesAsync();

            if (id != vm.Id)
                return BadRequest();

            vm.Title = (vm.Title ?? "").Trim();
            vm.ImageUrl = NormalizeImageUrl(vm.ImageUrl);

            var inv = await _db.Inventories
                .Include(i => i.Category)
                .Include(i => i.Creator)
                .Include(i => i.InventoryTags).ThenInclude(it => it.Tag)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inv is null)
                return NotFound();

            if (!await CanEditInventoryAsync(inv))
                return Forbid();

            InventoryDetailsVm BuildDetailsVmForSettings(Inventory inventory, InventoryUpsertVm settingsVm)
            {
                return new InventoryDetailsVm
                {
                    Id = inventory.Id,
                    Title = inventory.Title,
                    Description = inventory.Description,
                    DescriptionHtml = _markdown.ToSafeHtml(inventory.Description),
                    ImageUrl = inventory.ImageUrl,
                    CategoryName = inventory.Category?.Name ?? T["Common.Other"].Value,
                    IsPublic = inventory.IsPublic,
                    CreatorName = inventory.Creator?.Name ?? T["Common.Unknown"].Value,
                    CreatedAtUtc = inventory.CreatedAtUtc,
                    Tags = inventory.InventoryTags.Select(x => x.Tag.Name).OrderBy(x => x).ToList(),
                    ActiveTab = "settings",
                    CanEdit = true,
                    CanWrite = true,
                    IsAuthenticated = _currentUser.IsAuthenticated(User),
                    SettingsVm = settingsVm,
                };
            }

            if (string.IsNullOrWhiteSpace(vm.Title))
            {
                ModelState.AddModelError(nameof(vm.Title), T["Common.Required", T["Inv.Title"]].Value);
            }

            if (!ValidateImageFile(vm.ImageFile))
            {
                var invalidImageVm = BuildDetailsVmForSettings(inv, vm);
                return View("Details", invalidImageVm);
            }

            if (!ModelState.IsValid)
            {
                var invalidVm = BuildDetailsVmForSettings(inv, vm);
                return View("Details", invalidVm);
            }

            if (vm.Version != inv.Version)
            {
                ModelState.AddModelError("", T["Inv.Err.Concurrency"]);
                vm.Version = inv.Version;

                var conflictVm = BuildDetailsVmForSettings(inv, vm);
                return View("Details", conflictVm);
            }

            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == vm.CategoryId);
            if (!categoryExists)
            {
                ModelState.AddModelError(nameof(vm.CategoryId), T["Inv.Err.InvalidCategory"]);
                vm.Version = inv.Version;

                var invalidCategoryVm = BuildDetailsVmForSettings(inv, vm);
                return View("Details", invalidCategoryVm);
            }

            var imageUrl = await ResolveInventoryImageUrlAsync(vm);
            if (!ModelState.IsValid)
            {
                var invalidImageVm = BuildDetailsVmForSettings(inv, vm);
                return View("Details", invalidImageVm);
            }

            inv.Title = vm.Title;
            inv.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            inv.ImageUrl = imageUrl;
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

            try
            {
                inv.Version += 1;
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) 
            {
                ModelState.AddModelError("", T["Inv.Err.Concurrency"]);
                vm.Version = inv.Version;

                var saveConflictVm = BuildDetailsVmForSettings(inv, vm);
                return View("Details", saveConflictVm);
            }

            TempData["InventoryMessage"] = T["Inv.Edit.Saved"].Value;
            return RedirectToAction(nameof(Details), new { id = inv.Id, tab= "settings" });
        }       

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("{id:guid}/access/add")]
        public async Task<IActionResult> AccessAdd(Guid id, [FromForm] Guid? userId, [FromForm] bool canWrite)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == id);
            if (inv == null) return NotFound();
            if (!await CanEditInventoryAsync(inv)) return Forbid();

            if (!userId.HasValue || userId.Value == Guid.Empty)
            {
                TempData["InventoryMessage"] = T["Inv.Access.Err.SelectUser"].Value;
                return RedirectToAction(nameof(Details), new { id, tab = "access" });
            }

            if (userId.Value == inv.CreatorId)
            {
                TempData["InventoryMessage"] = T["Inv.Access.Err.OwnerAlreadyHasAccess"].Value;
                return RedirectToAction(nameof(Details), new { id, tab = "access" });
            }

            var user = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId.Value)
                .Select(u => new { u.Id, u.IsBlocked })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                TempData["InventoryMessage"] = T["Inv.Access.Err.UserNotFound"].Value;
                return RedirectToAction(nameof(Details), new { id, tab = "access" });
            }

            if (user.IsBlocked)
            {
                TempData["InventoryMessage"] = T["Inv.Access.Err.UserBlocked"].Value;
                return RedirectToAction(nameof(Details), new { id, tab = "access" });
            }

            var existing = await _db.InventoryAccesses
                .FirstOrDefaultAsync(a => a.InventoryId == id && a.UserId == user.Id);

            if (existing is null)
            {
                _db.InventoryAccesses.Add(new InventoryAccess
                {
                    InventoryId = id,
                    UserId = user.Id,
                    CanWrite = canWrite,
                    CreatedAtUtc = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();
                TempData["InventoryMessage"] = T["Inv.Access.Msg.Granted"].Value;
                return RedirectToAction(nameof(Details), new { id, tab = "access" });
            }

            existing.CanWrite = canWrite;
            await _db.SaveChangesAsync();

            TempData["InventoryMessage"] = T["Inv.Access.Msg.Updated"].Value;
            return RedirectToAction(nameof(Details), new { id, tab = "access" });
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("{id:guid}/access/remove")]
        public async Task<IActionResult> AccessRemove(Guid id, [FromForm] Guid[] ids)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == id);
            if (inv is null) return NotFound();
            if (!await CanEditInventoryAsync(inv)) return Forbid();

            if (ids == null || ids.Length == 0)
                return RedirectToAction(nameof(Details), new { id, tab = "access" });

            var rows = await _db.InventoryAccesses
                .Where(a => a.InventoryId == id && ids.Contains(a.UserId))
                .ToListAsync();

            if (rows.Count == 0)
                return RedirectToAction(nameof(Details), new { id, tab = "access" });

            _db.InventoryAccesses.RemoveRange(rows);
            await _db.SaveChangesAsync();

            TempData["InventoryMessage"] = T["Inv.Access.Msg.RemovedCount", rows.Count].Value;
            return RedirectToAction(nameof(Details), new { id, tab = "access" });
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("{id:guid}/access/set-write")]
        public async Task<IActionResult> AccessSetWrite(Guid id, [FromForm] Guid[] ids, [FromForm] bool canWrite)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == id);
            if (inv is null) return NotFound();
            if (!await CanEditInventoryAsync (inv)) return Forbid();

            if (ids == null || ids.Length == 0)
                return RedirectToAction(nameof(Details), new { id, tab = "access" });

            var rows = await _db.InventoryAccesses
                .Where(a => a.InventoryId == id && ids.Contains(a.UserId))
                .ToListAsync();

            if (rows.Count == 0)
                return RedirectToAction(nameof(Details), new { id, tab = "access" });

            foreach (var r in rows)
                r.CanWrite = canWrite;

            await _db.SaveChangesAsync();

            TempData["InventoryMessage"] = canWrite ? T["Inv.Access.Msg.GrantedWriteCount", rows.Count].Value : T["Inv.Access.Msg.SetReadOnlyCount", rows.Count].Value;

            return RedirectToAction(nameof(Details), new { id, tab = "access" });
        }

        [Authorize]
        [HttpGet("{id:guid}/access/suggest")]
        public async Task<IActionResult> AccessSuggest(Guid id, [FromQuery] string? q)
        {
            var inv = await _db.Inventories
                .AsNoTracking()
                .Select(i => new { i.Id, i.CreatorId })
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inv == null) return NotFound();

            var invEntity = new Inventory { Id = inv.Id, CreatorId = inv.CreatorId };
            if (!await CanEditInventoryAsync(invEntity)) return Forbid();

            var term = (q ?? "").Trim();
            if (string.IsNullOrWhiteSpace(term))
                return Json(Array.Empty<object>());

            if (term.Length > 80)
                term = term[..80];

            var prefixPattern = term + "%";

            var existing = _db.InventoryAccesses
                .AsNoTracking()
                .Where(a => a.InventoryId == id)
                .Select(a => a.UserId);

            var results = await _db.Users
                .AsNoTracking()
                .Where(u => !u.IsBlocked)
                .Where(u => u.Id != inv.CreatorId)
                .Where(u => !existing.Contains(u.Id))
                .Where(u => EF.Functions.ILike(u.Name, prefixPattern) || EF.Functions.ILike(u.Email, prefixPattern))
                .OrderBy(u => u.Name)
                .Take(10)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email
                })
                .ToListAsync();

            return Json(results);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("{id:guid}/customid")]
        public async Task<IActionResult> SaveCustomId(Guid id, InventoryCustomIdVm vm)
        {
            if (id != vm.InventoryId) return BadRequest();

            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == id);
            if (inv is null) return NotFound();

            if (!await CanEditInventoryAsync(inv)) return Forbid();

            if (vm.Version != inv.Version)
            {
                TempData["InventoryMessage"] = T["Inv.Err.Concurrency"].Value;
                return RedirectToAction(nameof(Details), new { id, tab = "customid" });
            }

            var prefix = (vm.Prefix ?? "").Trim();
            if (prefix.Length > 20) prefix = prefix[..20];

            prefix = string.IsNullOrWhiteSpace(prefix) ? "" : prefix;

            var digits = vm.Digits;
            if (digits < 1 || digits > 8) digits = 4;

            var next = vm.NextNumber;
            if (next < 1) next = 1;

            var reconciledNext = next;
            if (vm.Enabled)
            {
                reconciledNext = await GetSafeNextCustomIdNumberAsync(id, prefix, digits, next);
            }

            inv.ItemCustomIdEnabled = vm.Enabled;
            inv.ItemCustomIdPrefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix;
            inv.ItemCustomIdDigits = digits;
            inv.ItemCustomIdNextNumber = reconciledNext;

            inv.Version += 1;
            await _db.SaveChangesAsync();

            TempData["InventoryMessage"] = T["Inv.CustomId.Msg.Saved"].Value;
            return RedirectToAction(nameof(Details), new { id, tab = "customid" });
        }

        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Details(
            Guid id, [FromQuery] string? tab,
            [FromQuery] string ? accessSort,
            [FromQuery] string ? accessDir)
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

            bool hasAnyAccess = false;
            bool hasExplicitWriteAccess = false;

            if (!canEdit && isAuthenticated && userId.HasValue)
            {
                var access = await _db.InventoryAccesses
                    .AsNoTracking()
                    .Where(a => a.InventoryId == inv.Id && a.UserId == userId.Value)
                    .Select(a => new { a.CanWrite })
                    .FirstOrDefaultAsync();

                hasAnyAccess = access != null;
                hasExplicitWriteAccess = access?.CanWrite == true;
            }

            var canRead = inv.IsPublic || canEdit || hasAnyAccess;
            if (!canRead)
                return Forbid();

            var canWrite = canEdit || hasExplicitWriteAccess;

            var requestedTab = string.IsNullOrWhiteSpace(tab) ? "items" : tab.Trim().ToLowerInvariant();
            var allowedTabs = canEdit
                ? new[] { "items", "discussion", "settings", "customid", "fields", "access", "stats" }
                : new[] { "items", "discussion" };

            var activeTab = allowedTabs.Contains(requestedTab) ? requestedTab : "items";

            var vm = new InventoryDetailsVm
            {
                Id = inv.Id,
                Title = inv.Title,
                Description = inv.Description,
                DescriptionHtml = _markdown.ToSafeHtml(inv.Description),
                ImageUrl = inv.ImageUrl,
                CategoryName = inv.Category?.Name ?? T["Common.Other"].Value,
                IsPublic = inv.IsPublic,
                CreatorName = inv.Creator?.Name ?? T["Common.Unknown"].Value,
                CreatedAtUtc = inv.CreatedAtUtc,
                Tags = inv.InventoryTags.Select(x => x.Tag.Name).OrderBy(x => x).ToList(),
                ActiveTab = activeTab,
                CanEdit = canEdit,
                CanWrite = canWrite,
                IsAuthenticated = isAuthenticated,             
            };

            if (activeTab == "items")
            {
                vm.Items = await _db.Items
                    .AsNoTracking()
                    .Where(x => x.InventoryId == inv.Id)
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Take(500)
                    .Select(x => new ItemRowVm
                    {
                        Id = x.Id,
                        CustomId = x.CustomId,
                        Title = x.Title,
                        CreatedAtUtc = x.CreatedAtUtc,
                        UpdatedAtUtc = x.UpdatedAtUtc,
                        LikesCount = x.Likes.Count,
                        CommentsCount = x.Comments.Count
                    })
                    .ToListAsync();
            }

            if (activeTab == "access" && canEdit)
            {
                var sort = (accessSort ?? "name").Trim().ToLowerInvariant();
                var dir = (accessDir ?? "asc").Trim().ToLowerInvariant();

                if (sort is not ("name" or "email" or "write" or "granted"))
                    sort = "name";

                if (dir is not ("asc" or "desc"))
                    dir = "asc";

                IQueryable<InventoryAccess> query = _db.InventoryAccesses
                    .AsNoTracking()
                    .Include(a => a.User)
                    .Where(a => a.InventoryId == inv.Id);

                query = (sort, dir) switch
                {
                    ("name", "desc") => query.OrderByDescending(a => a.User != null ? a.User.Name : ""),
                    ("email", "asc") => query.OrderBy(a => a.User != null ? a.User.Email : ""),
                    ("email", "desc") => query.OrderByDescending(a => a.User != null ? a.User.Email : ""),
                    ("write", "asc") => query.OrderBy(a => a.CanWrite).ThenBy(a => a.User != null ? a.User.Name : ""),
                    ("write", "desc") => query.OrderByDescending(a => a.CanWrite).ThenBy(a => a.User != null ? a.User.Name : ""),
                    ("granted", "asc") => query.OrderBy(a => a.CreatedAtUtc),
                    ("granted", "desc") => query.OrderByDescending(a => a.CreatedAtUtc),
                    _ => query.OrderBy(a => a.User != null ? a.User.Name : "")
                };

                vm.AccessUsers = await query
                    .Take(500)
                    .Select(a => new InventoryAccessRowVm
                    {
                        UserId = a.User != null ? a.User.Id : Guid.Empty,
                        Name = a.User != null ? a.User.Name : T["Common.Unknown"].Value,
                        Email = a.User != null ? a.User.Email : "",
                        CanWrite = a.CanWrite,
                        CreatedAtUtc = a.CreatedAtUtc
                    })
                    .ToListAsync();

                ViewBag.AccessSort = sort;
                ViewBag.AccessDir = dir;
            }

            if (activeTab == "discussion")
            {
                vm.DiscussionComments = await _db.InventoryComments
                    .AsNoTracking()
                    .Where(c => c.InventoryId == inv.Id)
                    .OrderByDescending(c => c.CreatedAtUtc)
                    .Take(200)
                    .Select(c => new InventoryDiscussionCommentRowVm
                    {
                        Id = c.Id,
                        UserId = c.UserId,
                        UserName = c.User != null ? c.User.Name : T["Common.Unknown"].Value,
                        Body = c.Body,
                        CreatedAtUtc = c.CreatedAtUtc,
                        CanDelete = isAdmin || (isAuthenticated && userId.HasValue && (c.UserId == userId.Value || inv.CreatorId == userId.Value))
                    })
                    .ToListAsync();
            }

            if (activeTab == "settings" && canEdit)
            {
                await LoadCategoriesAsync();
                vm.SettingsVm = BuildInventoryUpsertVm(inv);
            }

            if (activeTab == "customid" && canEdit)
            {
                var prefix = (inv.ItemCustomIdPrefix ?? "").Trim();
                var digits = inv.ItemCustomIdDigits <= 0 ? 4 : inv.ItemCustomIdDigits;
                if (digits < 1) digits = 1;
                if (digits > 8) digits = 8;

                var next = inv.ItemCustomIdNextNumber <= 0 ? 1 : inv.ItemCustomIdNextNumber;

                vm.CustomIdVm = new InventoryCustomIdVm
                {
                    InventoryId = inv.Id,
                    Version = inv.Version,
                    Enabled = inv.ItemCustomIdEnabled,
                    Prefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix,
                    Digits = digits,
                    NextNumber = next,
                    Preview = BuildItemCustomId(prefix, digits, next)
                };
            }

            return View(vm);
        }

        private InventoryUpsertVm BuildInventoryUpsertVm(Inventory inv)
        {
            return new InventoryUpsertVm
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
        
        private static string BuildItemCustomId(string? prefix, int digits, int number)
        {
            var p = (prefix ?? "").Trim();
            if (digits < 1) digits = 1;
            if (digits > 8) digits = 8;
            if (number < 0) number = 0;

            var numeric = number.ToString().PadLeft(digits, '0');
            return string.IsNullOrWhiteSpace(p) ? numeric : $"{p}-{numeric}";
        }

        private async Task<int> GetSafeNextCustomIdNumberAsync(Guid inventoryId, string? prefix, int digits, int proposedNext)
        {
            if (proposedNext < 1) proposedNext = 1;
            if (digits < 1) digits = 1;
            if (digits > 8) digits = 8;

            var customIds = await _db.Items
                .AsNoTracking()
                .Where(x => x.InventoryId == inventoryId)
                .Select(x => x.CustomId)
                .ToListAsync();

            var maxUsedNumber = 0;

            foreach (var customId in customIds)
            {
                if (!TryExtractMatchingCustomIdNumber(customId, prefix, digits, out var number))
                    continue;

                if (number > maxUsedNumber)
                    maxUsedNumber = number;
            }

            var minSafeNext = maxUsedNumber + 1;
            return Math.Max(proposedNext, minSafeNext);
        }

        private static bool TryExtractMatchingCustomIdNumber(string? customId, string? prefix, int digits, out int number)
        {
            number = 0;

            var value = (customId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) 
                return false;

            var normalizedPrefix = (prefix ?? "").Trim();

            string numericPart;

            if (string.IsNullOrWhiteSpace(normalizedPrefix))
            {
                numericPart = value;
            }
            else
            {
                var expectedStart = normalizedPrefix + "-";
                if (!value.StartsWith(expectedStart, StringComparison.OrdinalIgnoreCase))
                    return false;

                numericPart = value.Substring(expectedStart.Length);
            }

            if (numericPart.Length != digits)
                return false;

            if (!numericPart.All(char.IsDigit))
                return false;

            return int.TryParse(numericPart, out number);
        }

        private bool ValidateImageFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return true;

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(InventoryUpsertVm.ImageFile), T["Inv.ImageUpload.InvalidType"].Value);
                return false;
            }

            if (file.ContentType == null || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(InventoryUpsertVm.ImageFile), T["Inv.ImageUpload.InvalidType"].Value);
                return false;
            }

            if (file.Length > MaxImageFileBytes)
            {
                ModelState.AddModelError(nameof(InventoryUpsertVm.ImageFile), T["Inv.ImageUpload.TooLarge", 5].Value);
                return false;
            }

            return true;
        }

        private async Task<string?> ResolveInventoryImageUrlAsync(InventoryUpsertVm vm)
        {
            if (vm.ImageFile == null || vm.ImageFile.Length == 0)
                return string.IsNullOrWhiteSpace(vm.ImageUrl) ? null : vm.ImageUrl.Trim();

            try
            {
                return await _inventoryImageService.UploadInventoryImageAsync(vm.ImageFile, HttpContext.RequestAborted);
            }
            catch
            {
                ModelState.AddModelError(nameof(InventoryUpsertVm.ImageFile), T["Inv.ImageUpload.UploadFailed"].Value);
                return null;
            }
        }

        private static string? NormalizeImageUrl(string? imageUrl)
        {
            var trimmed = (imageUrl ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

    }
}
