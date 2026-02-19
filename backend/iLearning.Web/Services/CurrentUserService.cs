using System.Security.Claims;

namespace iLearning.Web.Services
{
    public class CurrentUserService
    {
        public Guid? GetUserId(ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var guid) ? guid : null;
        }

        public bool IsAuthenticated(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true;

        public bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Admin");
    }
}
