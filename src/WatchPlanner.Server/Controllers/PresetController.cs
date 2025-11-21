using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MediaBrowser.Common.Configuration;

namespace WatchPlanner.Server.Controllers
{
    [ApiController]
    [Route("plugins/watchplanner/presets")]
    public class PresetController : ControllerBase
    {
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger<PresetController> _logger;
        private readonly string _presetPath;

        public PresetController(IApplicationPaths appPaths, ILogger<PresetController> logger)
        {
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var pluginFolder = ResolvePluginFolder(_appPaths);
            _presetPath = Path.Combine(pluginFolder, "preset.json");
        }

        // Reflection-based resolver to avoid depending on a specific IApplicationPaths shape
        private string ResolvePluginFolder(IApplicationPaths appPaths)
        {
            try
            {
                var t = appPaths.GetType();
                string? pluginData = null;

                // Common property names to try
                string[] tryProps = new[]
                {
                    "PluginDataPath",
                    "PluginDataDirectory",
                    "PluginData",
                    "PluginPath",
                    "ApplicationDataPath",
                    "ApplicationDataDirectory",
                    "ApplicationPaths",
                    "DataPath"
                };

                foreach (var name in tryProps)
                {
                    var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (p != null)
                    {
                        var val = p.GetValue(appPaths) as string;
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            pluginData = val;
                            break;
                        }
                    }
                }

                // Try common method names if properties didn't work
                if (pluginData == null)
                {
                    string[] tryMethods = new[] { "GetPluginDataPath", "GetDataPath", "GetApplicationDataPath", "GetApplicationPaths" };
                    foreach (var mName in tryMethods)
                    {
                        var mi = t.GetMethod(mName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (mi != null)
                        {
                            var val = mi.Invoke(appPaths, Array.Empty<object>()) as string;
                            if (!string.IsNullOrWhiteSpace(val))
                            {
                                pluginData = val;
                                break;
                            }
                        }
                    }
                }

                // Final fallback locations
                if (string.IsNullOrWhiteSpace(pluginData))
                {
                    var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    var baseDir = AppContext.BaseDirectory;
                    // prefer programData\Jellyfin if available, otherwise base directory
                    pluginData = !string.IsNullOrWhiteSpace(programData)
                        ? Path.Combine(programData, "Jellyfin", "data")
                        : Path.Combine(baseDir ?? ".", "data");
                }

                var folder = Path.Combine(pluginData, "watchplanner");

                try
                {
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not create plugin folder: {path}", folder);
                }

                _logger.LogInformation("[Watchplanner] Using plugin folder: {path}", folder);
                return folder;
            }
            catch (Exception ex)
            {
                // If reflection fails for some reason, fall back to a safe path and log
                var fallback = Path.Combine(AppContext.BaseDirectory ?? ".", "watchplanner-data");
                _logger.LogWarning(ex, "[Watchplanner] Failed to resolve plugin folder via IApplicationPaths reflection; falling back to {path}", fallback);
                try
                {
                    if (!Directory.Exists(fallback))
                        Directory.CreateDirectory(fallback);
                }
                catch { /* swallow - best effort */ }
                return fallback;
            }
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
