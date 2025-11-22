using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Jellyfin.Plugin.WatchPlannerSection.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.WatchPlannerSection.Controllers
{
    [ApiController]
    [Route("watchplanner")]
    public class WatchPlannerController : ControllerBase
    {
        private readonly WatchPlannerStorage _storage;

        // Preferred constructor when DI provides the storage
        public WatchPlannerController(WatchPlannerStorage storage)
        {
            _storage = storage;
        }

        // Fallback constructor if only IApplicationPaths is available
        public WatchPlannerController(IApplicationPaths applicationPaths)
        {
            _storage = new WatchPlannerStorage(applicationPaths);
        }

        [HttpGet("config")]
        [Authorize]
        public async Task<IActionResult> GetConfig()
        {
            var json = await _storage.ReadAsync().ConfigureAwait(false);
            return Content(json, "application/json");
        }

        [HttpPost("config")]
        [Authorize]
        public async Task<IActionResult> PostConfig([FromBody] object body)
        {
            if (!IsCurrentUserAdmin(User))
                return Forbid();

            var json = body?.ToString() ?? "{}";
            await _storage.WriteAsync(json).ConfigureAwait(false);
            return Ok();
        }

        // Robust admin detection that doesn't depend on server-specific APIs
        private static bool IsCurrentUserAdmin(ClaimsPrincipal user)
        {
            if (user == null || !user.Identity?.IsAuthenticated == true)
                return false;

            // 1) Standard Role claim
            if (user.IsInRole("Administrator") || user.IsInRole("Admin"))
                return true;

            // 2) ClaimTypes.Role or "role" claim
            var roleClaims = user.FindAll(ClaimTypes.Role).Select(c => c.Value)
                .Concat(user.FindAll("role").Select(c => c.Value));
            if (roleClaims.Any(r => r.Equals("Administrator") || r.Equals("Admin") || r.Equals("admin", System.StringComparison.OrdinalIgnoreCase)))
                return true;

            // 3) Jellyfin sometimes places "IsAdministrator" or similar claims
            var boolClaims = user.FindAll("IsAdministrator").Select(c => c.Value)
                .Concat(user.FindAll("isAdministrator").Select(c => c.Value))
                .Concat(user.FindAll("is_admin").Select(c => c.Value))
                .Concat(user.FindAll("isAdmin").Select(c => c.Value));
            if (boolClaims.Any(v => v == "true" || v == "True" || v == "1"))
                return true;

            // 4) Generic check: any claim containing "admin"
            if (user.Claims.Any(c => c.Value?.ToLowerInvariant().Contains("admin") == true))
                return true;

            return false;
        }
    }
}
