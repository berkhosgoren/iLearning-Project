using iLearning.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace iLearning.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? q)
        {
            var query = _db.Inventories
                .Include(i => i.Creator)
                .Include(i => i.Category)
                .Where(i => i.IsPublic)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim().ToLower();
                query = query.Where(i =>
                    i.Title.ToLower().Contains(search) ||
                    (i.Description ?? "").ToLower().Contains(search)
                );
            }

            var inventories = await query
                .OrderByDescending(i => i.CreatedAtUtc)
                .Take(50)
                .ToListAsync();

            return View(inventories);
        }
    }
}
