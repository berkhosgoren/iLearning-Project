using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using iLearning.Web.Data;
using iLearning.Web.Models.Domain;
using iLearning.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace iLearning.Web.Controllers
{
    [Route("integrations/odoo")]
    public class OdooController : Controller
    {
        private readonly AppDbContext _db;
        private readonly CurrentUserService _current;
        private readonly IStringLocalizer<SharedResource> T;

        public OdooController(AppDbContext db, CurrentUserService current, IStringLocalizer<SharedResource> t)
        {
            _db = db;
            _current = current;
            T = t;
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost("~/inventories/{inventoryId:guid}/odoo/generate-token")]
        public async Task<IActionResult> GenerateToken(Guid inventoryId)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(i => i.Id == inventoryId);
            if (inv == null)
                return NotFound();

            var userId = _current.GetUserId(User);
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var canEdit = _current.IsAdmin(User) || inv.CreatorId == userId.Value;
            if (!canEdit)
                return Forbid();

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            inv.OdooApiToken = token;
            inv.OdooApiTokenGeneratedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["OdooApiToken"] = token;
            TempData["InventoryMessage"] = T["Odoo.Token.Generated"].Value;

            return Redirect($"/inventories/{inventoryId}/stats");
        }

        [AllowAnonymous]
        [HttpGet("inventory")]
        public async Task<IActionResult> ExportInventory([FromQuery] string? token)
        {
            token = (token ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized();

            var inv = await _db.Inventories
                .AsNoTracking()
                .Include(i => i.Category)
                .Include(i => i.Creator)
                .FirstOrDefaultAsync(i => i.OdooApiToken == token);

            if (inv == null)
                return Unauthorized();

            var items = await _db.Items
                .AsNoTracking()
                .Where(x => x.InventoryId == inv.Id)
                .ToListAsync();

            var likesTotal = await _db.ItemLikes
                .AsNoTracking()
                .CountAsync(l => l.Item.InventoryId == inv.Id);

            var commentsTotal = await _db.ItemComments
                .AsNoTracking()
                .CountAsync(c => c.Item.InventoryId == inv.Id);

            var fields = GetEnabledFields(inv);
            var aggregates = BuildAggregates(fields, items);

            var payload = new
            {
                inventoryId = inv.Id,
                title = inv.Title,
                category = inv.Category?.Name ?? T["Common.Other"].Value,
                owner = inv.Creator?.Name ?? T["Common.Unknown"].Value,
                generatedAtUtc = DateTime.UtcNow.ToString("dd-MM-yyyy HH:mm:ss 'UTC'"),
                summary = new
                {
                    itemsTotal = items.Count,
                    likesTotal,
                    commentsTotal,
                    lastItemCreatedAtUtc = items.Count == 0 ? null : items.Max(x => x.CreatedAtUtc).ToString("dd-MM-yyyy HH:mm:ss 'UTC'"),
                    lastItemUpdatedAtUtc = items.Where(x => x.UpdatedAtUtc.HasValue).Select(x => x.UpdatedAtUtc!.Value).DefaultIfEmpty().Max() == default
                        ? null
                        : items.Where(x => x.UpdatedAtUtc.HasValue).Max(x => x.UpdatedAtUtc)!.Value.ToString("dd-MM-yyyy HH:mm:ss 'UTC'")
                },
                fields,
                aggregates
            };

            return Json(payload);
        }

        private static List<object> GetEnabledFields(Inventory inv)
        {
            var fields = new List<object>();

            void Add(bool enabled, string key, string? title, string type)
            {
                if (!enabled) return;

                fields.Add(new
                {
                    key,
                    title = string.IsNullOrWhiteSpace(title) ? key : title,
                    type
                });
            }

            Add(inv.CustomString1Enabled, "string1", inv.CustomString1Name, "string");
            Add(inv.CustomString2Enabled, "string2", inv.CustomString2Name, "string");
            Add(inv.CustomString3Enabled, "string3", inv.CustomString3Name, "string");

            Add(inv.CustomText1Enabled, "text1", inv.CustomText1Name, "text");
            Add(inv.CustomText2Enabled, "text2", inv.CustomText2Name, "text");
            Add(inv.CustomText3Enabled, "text3", inv.CustomText3Name, "text");

            Add(inv.CustomNumber1Enabled, "number1", inv.CustomNumber1Name, "number");
            Add(inv.CustomNumber2Enabled, "number2", inv.CustomNumber2Name, "number");
            Add(inv.CustomNumber3Enabled, "number3", inv.CustomNumber3Name, "number");

            Add(inv.CustomBool1Enabled, "bool1", inv.CustomBool1Name, "bool");
            Add(inv.CustomBool2Enabled, "bool2", inv.CustomBool2Name, "bool");
            Add(inv.CustomBool3Enabled, "bool3", inv.CustomBool3Name, "bool");

            Add(inv.CustomLink1Enabled, "link1", inv.CustomLink1Name, "link");
            Add(inv.CustomLink2Enabled, "link2", inv.CustomLink2Name, "link");
            Add(inv.CustomLink3Enabled, "link3", inv.CustomLink3Name, "link");

            return fields;
        }

        private static List<object> BuildAggregates(List<object> fields, List<Item> items)
        {
            var results = new List<object>();

            foreach (dynamic field in fields)
            {
                string key = field.key;
                string title = field.title;
                string type = field.type;

                if (type == "number")
                {
                    var numbers = items
                        .Select(x => GetNumberValue(x, key))
                        .Where(x => x.HasValue)
                        .Select(x => x!.Value)
                        .ToList();

                    results.Add(new
                    {
                        key,
                        title,
                        type,
                        minNumber = numbers.Count == 0 ? (decimal?)null : numbers.Min(),
                        maxNumber = numbers.Count == 0 ? (decimal?)null : numbers.Max(),
                        avgNumber = numbers.Count == 0 ? (decimal?)null : Math.Round(numbers.Average(), 2)
                    });

                    continue;
                }

                if (type == "bool")
                {
                    var values = items.Select(x => GetBoolValue(x, key)).ToList();

                    results.Add(new
                    {
                        key,
                        title,
                        type,
                        trueCount = values.Count(x => x == true),
                        falseCount = values.Count(x => x == false)
                    });

                    continue;
                }

                var topValues = items
                    .Select(x => GetStringValue(x, key))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!.Trim())
                    .GroupBy(x => x)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .Take(5)
                    .Select(g => new
                    {
                        value = g.Key.Length > 200 ? g.Key[..200] : g.Key,
                        count = g.Count()
                    })
                    .ToList();

                results.Add(new
                {
                    key,
                    title,
                    type,
                    topValues
                });
            }

            return results;
        }

        private static decimal? GetNumberValue(Item item, string key) => key switch
        {
            "number1" => item.Number1,
            "number2" => item.Number2,
            "number3" => item.Number3,
            _ => null
        };

        private static bool? GetBoolValue(Item item, string key) => key switch
        {
            "bool1" => item.Bool1,
            "bool2" => item.Bool2,
            "bool3" => item.Bool3,
            _ => null
        };

        private static string? GetStringValue(Item item, string key) => key switch
        {
            "string1" => item.String1,
            "string2" => item.String2,
            "string3" => item.String3,
            "text1" => item.Text1,
            "text2" => item.Text2,
            "text3" => item.Text3,
            "link1" => item.Link1,
            "link2" => item.Link2,
            "link3" => item.Link3,
            _ => null
        };
    }
}
