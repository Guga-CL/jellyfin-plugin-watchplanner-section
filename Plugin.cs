using System;
using System.Linq;
using System.Reflection;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.WatchPlannerSection
{
    public class Plugin : BasePlugin
    {
        private readonly ILogger<Plugin> _logger;

        public override string Name => "Watch Planner Section";
        public override string Description => "Global watch planner UI injected on home; admin-editable, persisted in plugin config directory.";
        public override Guid Id => Guid.Parse("7b5f2a58-4c2b-4f2f-9f5a-0a6a9f1a9b20");

        public Plugin(ILogger<Plugin> logger)
        {
            _logger = logger;

            try
            {
                TryRegisterInjector();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("WatchPlanner: Startup injection skipped due to error: {Message}", ex.Message);
            }
        }

        private void TryRegisterInjector()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                var injectorType = assemblies
                    .SelectMany(a => SafeGetTypes(a))
                    .FirstOrDefault(t =>
                        t.FullName?.Contains("JavaScriptInjector", StringComparison.OrdinalIgnoreCase) == true ||
                        t.FullName?.Contains("FileTransformation", StringComparison.OrdinalIgnoreCase) == true);

                if (injectorType == null)
                {
                    _logger.LogInformation("WatchPlanner: No injector plugin detected. UI injection will not be automatic.");
                    return;
                }

                var registerMethod = injectorType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name.Contains("Register", StringComparison.OrdinalIgnoreCase));

                if (registerMethod == null)
                {
                    _logger.LogWarning("WatchPlanner: Injector found, but no Register* method available. Skipping.");
                    return; // <-- guard prevents NullReferenceException
                }

                var scriptTag = "<script src=\"/web/plugins/Watch Planner Section_0.0.0.1/watchplanner/plugin-client.js\"></script>";

                if (registerMethod.GetParameters().Length == 2)
                {
                    registerMethod.Invoke(registerMethod.IsStatic ? null : Activator.CreateInstance(injectorType),
                        new object[] { "index.html", scriptTag });
                }
                else if (registerMethod.GetParameters().Length == 1)
                {
                    registerMethod.Invoke(registerMethod.IsStatic ? null : Activator.CreateInstance(injectorType),
                        new object[] { scriptTag });
                }

                _logger.LogInformation("WatchPlanner: Registered client script with injector successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("WatchPlanner: Injection registration failed: {Message}", ex.Message);
            }
        }

        private static Type[] SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch { return Array.Empty<Type>(); }
        }
    }
}
