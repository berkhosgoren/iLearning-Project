using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Support;
using iLearning.Web.Services;
using iLearning.Web.Services.Dropbox;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;


namespace iLearning.Web.Controllers
{

    [Authorize]
    [Route("support")]
    public class SupportTicketsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;
        private readonly IDropboxTicketUploadService _dropbox;
        private readonly IStringLocalizer<SharedResource> T;
        private readonly ILogger<SupportTicketsController> _logger;

        public SupportTicketsController(AppDbContext db, CurrentUserService current, IDropboxTicketUploadService dropbox, IStringLocalizer<SharedResource> t, ILogger<SupportTicketsController> logger)
        {
            _db = db;
            _current = current;
            _dropbox = dropbox;
            T = t;
            _logger = logger;
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create(string? returnUrl, Guid? inventoryId)
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            var safeReturnUrl = NormalizeReturnUrl(returnUrl);
            var adminEmails = await GetAdminEmailsAsync();
            var inventoryTitle = await ResolveInventoryTitleAsync(inventoryId);
            var currentPageUrl = BuildAbsoluteUrl(safeReturnUrl);

            var vm = new SupportTicketVm
            {
                Priority = "Average",
                ReturnUrl = safeReturnUrl,
                InventoryId = inventoryId,
                ReportedByName = user.Name,
                ReportedByEmail = user.Email,
                InventoryTitle = inventoryTitle,
                CurrentPageUrl = currentPageUrl,
                AdminEmailCsv = string.Join(", ", adminEmails)
            };

            return View(vm);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupportTicketVm vm)
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            vm.Summary = (vm.Summary ?? string.Empty).Trim();
            vm.Priority = NormalizePriority(vm.Priority);

            var safeReturnUrl = NormalizeReturnUrl(vm.ReturnUrl);
            var adminEmails = await GetAdminEmailsAsync();
            var inventoryTitle = await ResolveInventoryTitleAsync(vm.InventoryId);
            var currentPageUrl = BuildAbsoluteUrl(safeReturnUrl);

            vm.ReturnUrl = safeReturnUrl;
            vm.ReportedByName = user.Name;
            vm.ReportedByEmail = user.Email;
            vm.InventoryTitle = inventoryTitle;
            vm.CurrentPageUrl = currentPageUrl;
            vm.AdminEmailCsv = string.Join(", ", adminEmails);

            if (!ModelState.IsValid)
                return View(vm);

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError(string.Empty, T["Support.Errors.MissingUserEmail"].Value);
                return View(vm);
            }

            if (adminEmails.Count == 0)
            {
                ModelState.AddModelError(string.Empty, T["Support.Errors.NoAdminEmails"].Value);
                return View(vm);
            }

            var payload = new SupportTicketJson
            {
                Summary = vm.Summary,
                Priority = vm.Priority,
                ReportedBy = user.Name,
                ReportedByEmail = user.Email,
                Inventory = inventoryTitle,
                Link = currentPageUrl,
                AdminEmails = adminEmails,
                CreatedAtUtc = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss 'UTC'")
            };

            var fileName = $"support-ticket-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json";
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            try
            {
                await _dropbox.UploadSupportTicketAsync(new DropboxUploadRequest
                {
                    FileName = fileName,
                    JsonContent = json
                }, HttpContext.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Support ticket upload to Dropbox failed for user {UserId}", userId.Value);
                ModelState.AddModelError(string.Empty, T["Support.Errors.UploadFailed"].Value);
                return View(vm);
            }

            TempData["SupportMessage"] = T["Support.Success"].Value;
            return Redirect(safeReturnUrl);
        }

        private async Task<List<string>> GetAdminEmailsAsync()
        {
            return await _db.Users
                .AsNoTracking()
                .Where(u => !string.IsNullOrWhiteSpace(u.Email) &&
                            u.UserRoles.Any(ur => ur.Role.Name == "Admin"))
                .Select(u => u.Email)
                .Distinct()
                .OrderBy(e => e)
                .ToListAsync();
        }

        private async Task<string?> ResolveInventoryTitleAsync(Guid? inventoryId)
        {
            if (!inventoryId.HasValue)
                return null;

            return await _db.Inventories
                .AsNoTracking()
                .Where(i => i.Id == inventoryId.Value)
                .Select(i => i.Title)
                .FirstOrDefaultAsync();
        }

        private string NormalizeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                return Url.Action("Index", "Home") ?? "/";
            }

            if (returnUrl.StartsWith("/support", StringComparison.OrdinalIgnoreCase))
            {
                return Url.Action("Index", "Home") ?? "/";
            }

            return returnUrl;
        }

        private string BuildAbsoluteUrl(string localUrl)
        {
            return $"{Request.Scheme}://{Request.Host}{localUrl}";
        }

        private static string NormalizePriority(string? priority)
        {
            var value = (priority ?? string.Empty).Trim();

            return value switch
            {
                "High" => "High",
                "Low" => "Low",
                _ => "Average"
            };
        }

        private class SupportTicketJson
        {
            [JsonPropertyName("Summary")]
            public string Summary { get; set; } = string.Empty;

            [JsonPropertyName("Priority")]
            public string Priority { get; set; } = string.Empty;

            [JsonPropertyName("Reported by")]
            public string ReportedBy { get; set; } = string.Empty;

            [JsonPropertyName("Reported by email")]
            public string ReportedByEmail { get; set; } = string.Empty;

            [JsonPropertyName("Inventory")]
            public string? Inventory { get; set; }

            [JsonPropertyName("Link")]
            public string Link { get; set; } = string.Empty;

            [JsonPropertyName("Admin emails")]
            public List<string> AdminEmails { get; set; } = new();

            [JsonPropertyName("Created at UTC")]
            public string CreatedAtUtc { get; set; } = string.Empty;
        }
    }
}
