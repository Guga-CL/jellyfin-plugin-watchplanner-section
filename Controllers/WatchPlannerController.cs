using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.WatchPlannerSection.Controllers
{
    [ApiController]
    [Route("watchplanner")]
    public class WatchPlannerController : ControllerBase
    {
        private readonly ILogger<WatchPlannerController> _logger;
        private readonly IApplicationPaths _appPaths;

        public WatchPlannerController(IApplicationPaths appPaths, ILogger<WatchPlannerController> logger)
        {
            _appPaths = appPaths;
            _logger = logger;
        }

        private string ConfigFilePath =>
            Path.Combine(_appPaths.PluginConfigurationsPath, "watchplanner-config.json");

        [HttpGet("config")]
        [Authorize]
        public IActionResult GetConfig()
        {
            try
            {
                if (!System.IO.File.Exists(ConfigFilePath))
                {
                    var empty = new { schedule = new System.Collections.Generic.Dictionary<string, object>() };
                    return new JsonResult(empty);
                }

                var json = System.IO.File.ReadAllText(ConfigFilePath);
                return Content(json, "application/json");
            }
            catch (IOException io)
            {
                _logger.LogError(io, "WatchPlanner: Error reading config.");
                return StatusCode(500, new { error = "Failed to read config." });
            }
        }

        [HttpPost("config")]
        [Authorize]
        public IActionResult SaveConfig([FromBody] JsonElement body)
        {
            try
            {
                if (!User.IsInRole("Administrator"))
                    return Forbid();

                Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath)!);
                System.IO.File.WriteAllText(ConfigFilePath, body.ToString());
                _logger.LogInformation("WatchPlanner: Config updated by {User}.", User.Identity?.Name ?? "unknown");

                return Ok(new { status = "ok" });
            }
            catch (IOException io)
            {
                _logger.LogError(io, "WatchPlanner: Error writing config.");
                return StatusCode(500, new { error = "Failed to write config." });
            }
        }
    }
}
