// PresetController.cs
using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller; // can be useful if your Jellyfin package exposes types here using Jellyfin.Server; // include if your Jellyfin references expose types under this 
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Newtonsoft.Json.Linq;

namespace WatchPlanner.Server.Controllers
{
    [Route("/plugins/watchplanner/presets")]
    [ApiController]
    public class PresetController : ControllerBase
    {
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger<PresetController> _logger;
        private readonly string _presetPath;

    public PresetController(IApplicationPaths appPaths, ILogger<PresetController> logger)
    {
        _appPaths = appPaths;
        _logger = logger;

        var pluginFolder = Path.Combine(_appPaths.PluginDataPath ?? _appPaths.ApplicationDataPath, "watchplanner");
        try
        {
            if (!Directory.Exists(pluginFolder))
                Directory.CreateDirectory(pluginFolder);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create plugin folder: {path}", pluginFolder);
        }
        _presetPath = Path.Combine(pluginFolder, "preset.json");
    }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetPreset()
        {
            try
            {
                if (!System.IO.File.Exists(_presetPath))
                {
                    var defaultJson = "{\"serverWeekGrid\":{}}";
                    return Content(defaultJson, "application/json");
                }

                var json = await System.IO.File.ReadAllTextAsync(_presetPath).ConfigureAwait(false);
                return Content(string.IsNullOrWhiteSpace(json) ? "{\"serverWeekGrid\":{}}" : json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading watchplanner preset");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "read_failed" });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SavePreset([FromBody] object payload)
        {
            try
            {
                var user = HttpContext.User;
                var isAdmin = user?.IsInRole("Administrator") ?? false;
                if (!isAdmin)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "forbidden" });
                }

                if (payload == null)
                {
                    return BadRequest(new { error = "empty_payload" });
                }

                var json = payload.ToString();
                await System.IO.File.WriteAllTextAsync(_presetPath, json).ConfigureAwait(false);
                return Ok(new { status = "ok" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving watchplanner preset");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "write_failed" });
            }
        }
    }
}
