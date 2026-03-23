using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Salesforce;
using iLearning.Web.Services;
using iLearning.Web.Services.Salesforce;

namespace iLearning.Web.Controllers
{
    [Authorize]
    [Route("account/salesforce")]
    public class SalesforceController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;
        private readonly ISalesforceCrmService _salesforce;
        private readonly IStringLocalizer<SharedResource> T;

        public SalesforceController(AppDbContext db, CurrentUserService current, ISalesforceCrmService salesforce, IStringLocalizer<SharedResource> t)
        {
            _db = db;
            _current = current;
            _salesforce = salesforce;
            T = t;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            var (firstName, lastName) = SplitName(user.Name);

            var vm = new SalesforceExportVm
            {
                AccountName = user.Name,
                FirstName = firstName,
                LastName = lastName,
                Email = user.Email
            };

            return View(vm);
        }

        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SalesforceExportVm vm)
        {
            var userId = _current.GetUserId(User);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var user = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError(string.Empty, T["Salesforce.Errors.MissingUserEmail"].Value);
                return View(vm);
            }

            vm.Email = user.Email;

            vm.AccountName = (vm.AccountName ?? string.Empty).Trim();
            vm.FirstName = string.IsNullOrWhiteSpace(vm.FirstName) ? null : vm.FirstName.Trim();
            vm.LastName = (vm.LastName ?? string.Empty).Trim();
            vm.Phone = string.IsNullOrWhiteSpace(vm.Phone) ? null : vm.Phone.Trim();
            vm.Title = string.IsNullOrWhiteSpace(vm.Title) ? null : vm.Title.Trim();
            vm.Website = string.IsNullOrWhiteSpace(vm.Website) ? null : vm.Website.Trim();
            vm.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();

            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                await _salesforce.CreateAccountWithContactAsync(new SalesforceCreateRequest
                {
                    AccountName = vm.AccountName,
                    FirstName = vm.FirstName ?? string.Empty,
                    LastName = vm.LastName,
                    Email = user.Email,
                    Phone = vm.Phone,
                    Title = vm.Title,
                    Website = vm.Website,
                    Description = vm.Description,
                }, HttpContext.RequestAborted);
            }
            catch (Exception ex) 
            {
                ModelState.AddModelError(string.Empty, T["Salesforce.Errors.ExportFailed"].Value + " " + ex.Message);
                return View(vm);
            }

            TempData["AccountMessage"] = T["Salesforce.Success"].Value;
            return RedirectToAction("Index", "Account");
        }

        private static (string? FirstName, string LastName) SplitName(string fullName)
        {
            var value = (fullName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
                return (null, "User");

            var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return (null, parts[0]);

            var lastName = parts[^1];
            var firstName = string.Join(' ', parts.Take(parts.Length - 1));
            return (firstName, lastName);
        }
    }
}
