param(
    [string]$ProjectDir = "src/My.Custom.Section.Plugin",
    [string]$Namespace = "My.Custom.Section.Plugin",
    [string]$Guid = "11111111-2222-3333-4444-555555555555",
    [string]$DisplayText = "My Custom Section"
)

if (-not (Test-Path $ProjectDir)) {
    Write-Host "Project directory '$ProjectDir' not found. Creating..."
    New-Item -ItemType Directory -Path $ProjectDir -Force | Out-Null
}

# Single-quoted here-string template (no variable expansion) with placeholders {NAMESPACE}, {GUID}, {DISPLAY}
$pluginTemplate = @'
using System;
using System.Linq;
using System.Runtime.Loader;
using System.Text.Json;

namespace {NAMESPACE}
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
                id = "{GUID}",
                displayText = "{DISPLAY}",
                limit = 1,
                route = "",
                additionalData = "",
                resultsAssembly = this.GetType().Assembly.FullName,
                resultsClass = "{NAMESPACE}.ResultsHandler",
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
            registerMethod.Invoke(null, new object[] { payloadObj });

            Console.WriteLine("RegisterSection invoked on HomeScreenSections plugin.");
        }
    }
}
'@

$pluginContent = $pluginTemplate -replace '\{NAMESPACE\}',$Namespace -replace '\{GUID\}',$Guid -replace '\{DISPLAY\}',$DisplayText
$pluginPath = Join-Path $ProjectDir "Plugin.cs"
Set-Content -Path $pluginPath -Value $pluginContent -Encoding UTF8
Write-Host "Created or updated: $pluginPath"

# ResultsHandler template (uses real types; requires MediaBrowser packages)
$resultsTemplate = @'
using System.Collections.Generic;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Entities;

namespace {NAMESPACE}
{
    public class ResultsHandler
    {
        // Entry point called by HomeScreenSections. Must return QueryResult<BaseItemDto>.
        public static QueryResult<BaseItemDto> GetSectionResults(object request)
        {
            var result = new QueryResult<BaseItemDto>
            {
                Items = new List<BaseItemDto>(),
                TotalRecordCount = 0
            };
            return result;
        }
    }
}
'@

$resultsContent = $resultsTemplate -replace '\{NAMESPACE\}',$Namespace
$resultsPath = Join-Path $ProjectDir "ResultsHandler.cs"
Set-Content -Path $resultsPath -Value $resultsContent -Encoding UTF8
Write-Host "Created or updated: $resultsPath"

# SectionRegistrar template
$registrarTemplate = @'
using System.Text.Json;

namespace {NAMESPACE}
{
    public static class SectionRegistrar
    {
        public static object BuildPayload(string id, string displayText, string resultsAssembly, string resultsClass, string resultsMethod)
        {
            var payload = new {
                id = id,
                displayText = displayText,
                limit = 1,
                route = "",
                additionalData = "",
                resultsAssembly = resultsAssembly,
                resultsClass = resultsClass,
                resultsMethod = resultsMethod
            };

            return JsonSerializer.Deserialize<object>(JsonSerializer.Serialize(payload));
        }
    }
}
'@

$registrarContent = $registrarTemplate -replace '\{NAMESPACE\}',$Namespace
$registrarPath = Join-Path $ProjectDir "SectionRegistrar.cs"
Set-Content -Path $registrarPath -Value $registrarContent -Encoding UTF8
Write-Host "Created or updated: $registrarPath"

Write-Host "All files created/updated in $ProjectDir. Re-run with different parameters to change namespace/GUID/display text."
