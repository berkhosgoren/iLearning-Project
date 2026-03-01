using Microsoft.AspNetCore.Mvc;
using iLearning.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace iLearning.Web.Controllers
{
    [AllowAnonymous]
    [Route("tags")]
    public class TagsController : Controller
    {
        private readonly AppDbContext _db;

        public TagsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest([FromQuery] string? q)
        {
            var term = (q ?? "").Trim();
            if (term.Length < 2)
                return Json(Array.Empty<object>());

            if (term.Length > 60)
                term = term[..60];

            var pattern = term + "%";

            var results = await _db.Tags
                .AsNoTracking()
                .Where(t => EF.Functions.ILike(t.Name, pattern))
                .OrderBy(t => t.Name)
                .Take(12)
                .Select(t => new { t.Id, t.Name })
                .ToListAsync();

            return Json(results);
        }
    }
}
