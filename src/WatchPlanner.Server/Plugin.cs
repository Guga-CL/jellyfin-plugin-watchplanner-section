using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace WatchPlanner.Server
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        private readonly ILogger<Plugin> _logger;

        public override Guid Id => new Guid("7f766bcf-d759-4037-96bf-47a4115b12b5");
        public override string Name => "Watchplanner Section";
        public override string Description => "Adds a custom Watchplanner Section to the Jellyfin home screen.";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ILogger<Plugin> logger)
            : base(applicationPaths, xmlSerializer)
        {
            _logger = logger;
            _logger.LogInformation("[Watchplanner] Plugin constructor running...");

            // Single delayed registration
            System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5));
                RegisterSectionSafe();
            });
        }

        private void RegisterSectionSafe()
        {
            try
            {
                var assemblyName = typeof(SectionResults).Assembly.GetName().Name;
                var resultsClass = typeof(SectionResults).FullName;
                var resultsMethod = nameof(SectionResults.GetResults);

                var payload = new JObject
                {
                    ["id"] = "myCustomSection",
                    ["displayText"] = "My Custom Section",
                    ["limit"] = 1,
                    ["route"] = "",
                    ["additionalData"] = "",
                    // ["type"] = "cards",
                    // ["sectionType"] = "CustomSection",
                    // ["category"] = "Custom",
                    // ["order"] = 99,
                    // ["enabledByDefault"] = true,
                    // ["ViewMode"] = "Portrait",
                    // ["DisplayTitleText"] = true,
                    // ["ShowDetailsMenu"] = true,
                    // ["AllowViewModeChange"] = true,
                    // ["AllowHideWatched"] = true,
                    ["resultsAssembly"] = assemblyName,
                    ["resultsClass"] = resultsClass,
                    ["resultsMethod"] = resultsMethod
                };

                var hsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.HomeScreenSections");
                var pluginInterfaceType = hsAssembly?.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
                var registerMethod = pluginInterfaceType?.GetMethod("RegisterSection", BindingFlags.Public | BindingFlags.Static);

                if (registerMethod == null)
                {
                    _logger.LogError("[MyCustomSection] ERROR: RegisterSection method not found.");
                    return;
                }

                _logger.LogInformation("[MyCustomSection] RegisterSection payload: {Payload}", payload.ToString(Newtonsoft.Json.Formatting.None));
                registerMethod.Invoke(null, [payload]);
                _logger.LogInformation("[MyCustomSection] Section registered successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MyCustomSection] ERROR during registration");
            }
        }
    }
}

