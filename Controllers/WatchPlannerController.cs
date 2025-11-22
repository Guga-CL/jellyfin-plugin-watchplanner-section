using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.WatchPlannerSection.Controllers
{
    [ApiController]
    [Route("watchplanner")]
    public class WatchPlannerController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping() => Ok(new { ok = true });

        // Add your real endpoints here, but keep constructors empty and avoid static initializers.
    }
}
