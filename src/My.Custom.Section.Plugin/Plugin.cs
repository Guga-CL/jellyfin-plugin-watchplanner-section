using System;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;

namespace My.Custom.Section.Plugin
{
    // Minimal plugin bootstrap that attempts to register a Home Screen Section.
    public class PluginBootstrap
    {
        public PluginBootstrap()
        {
            try
            {
                RegisterSectionOnStartup();
                Console.WriteLine("PluginBootstrap: attempted to register section.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PluginBootstrap: error registering section: {ex}");
            }
        }

        private void RegisterSectionOnStartup()
        {
            var payload = new
            {
                id = "11111111-2222-3333-4444-555555555555",
                displayText = "My Custom Section",
                limit = 1,
                route = "",
                additionalData = "",
                resultsAssembly = this.GetType().Assembly.FullName,
                resultsClass = "My.Custom.Section.Plugin.ResultsHandler",
                resultsMethod = "GetSectionResults"
            };

            var homeScreenSectionsAssembly = AssemblyLoadContext
                .All
                .SelectMany(ctx => ctx.Assemblies)
                .FirstOrDefault(a => a.FullName?.Contains(".HomeScreenSections") ?? false);

            if (homeScreenSectionsAssembly == null)
            {
                Console.WriteLine("HomeScreenSections assembly not found in loaded contexts. Ensure the Modular Home plugin is installed and loaded.");
                return;
            }

            var pluginInterfaceType = homeScreenSectionsAssembly.GetType("Jellyfin.Plugin.HomeScreenSections.PluginInterface");
            var registerMethod = pluginInterfaceType?.GetMethod("RegisterSection");

            if (registerMethod == null)
            {
                Console.WriteLine("RegisterSection method not found on PluginInterface.");
                return;
            }

            var payloadObj = JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(payload));
            if (payloadObj == null)
            {
                Console.WriteLine("Failed to build payload object.");
                return;
            }
            registerMethod.Invoke(null, new object[] { payloadObj });


            Console.WriteLine("RegisterSection invoked on HomeScreenSections plugin.");
        }
    }
}
