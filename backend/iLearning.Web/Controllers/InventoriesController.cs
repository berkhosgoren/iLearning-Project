using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using iLearning.Web.Models.ViewModels.Inventories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iLearning.Web.Controllers
{
    [Route("inventories")]
    public class InventoriesController : Controller
    {
        private readonly AppDbContext _db;

        public InventoriesController(AppDbContext db)
        {
            _db = db; 
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
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "items" : tab.Trim().ToLowerInvariant()
            };

            return View(vm);
        }
    
    }
}
