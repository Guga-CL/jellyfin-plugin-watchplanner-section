using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.WatchPlannerSection.Configuration;

namespace Jellyfin.Plugin.WatchPlannerSection
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public override string Name => "Watch Planner Section";
        public override string Description => "Global watch planner UI injected on home; admin-editable, persisted in plugin config directory.";
        public override Guid Id => Guid.Parse("631fa65f-0afb-4933-8026-cf41eefc6444");

        // Keep constructor signature exactly like this for Jellyfin DI
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            // Do not perform heavy synchronous work here.
            // Fire-and-forget a safe registration for FileTransformation so we don't depend on compile-time types.
            TryRegisterFileTransformationAsync(applicationPaths).ConfigureAwait(false);
        }

        // Async, tolerant registration attempt (non-blocking, swallowed exceptions)
        private static async Task TryRegisterFileTransformationAsync(IApplicationPaths applicationPaths)
        {
            await Task.Yield(); // ensure we run asynchronously and not block plugin construction

            try
            {
                // Attempt to find the FileTransformation assembly already loaded by Jellyfin
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "Jellyfin.Plugin.FileTransformation", StringComparison.OrdinalIgnoreCase));

                if (asm == null)
                {
                    // If not loaded yet, give it a short delay and try again once more
                    await Task.Delay(500).ConfigureAwait(false);
                    asm = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => string.Equals(a.GetName().Name, "Jellyfin.Plugin.FileTransformation", StringComparison.OrdinalIgnoreCase));
                    if (asm == null) return;
                }

                // Try common types that the plugin exposes (best-effort)
                var ftTypes = new[]
                {
                    "Jellyfin.Plugin.FileTransformation.FileTransformationService",
                    "Jellyfin.Plugin.FileTransformation.FileTransformationPlugin",
                    "Jellyfin.Plugin.FileTransformation.FileTransformationManager",
                    "Jellyfin.Plugin.FileTransformation.IFileTransformationService"
                };

                Type serviceType = null;
                foreach (var tn in ftTypes)
                {
                    serviceType = asm.GetType(tn);
                    if (serviceType != null) break;
                }

                if (serviceType == null) return;

                // Try to locate a Register method. Different versions use varying names/signatures.
                var registerMethod = serviceType.GetMethods()
                    .FirstOrDefault(m => string.Equals(m.Name, "RegisterTransformation", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(m.Name, "Register", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(m.Name, "RegisterClientScript", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(m.Name, "RegisterTransformationAsync", StringComparison.OrdinalIgnoreCase));

                if (registerMethod == null)
                {
                    // Some plugins expose a static Instance or Service property to call on; try common patterns
                    var prop = serviceType.GetProperty("Instance") ?? serviceType.GetProperty("Service");
                    object instance = null;
                    if (prop != null)
                    {
                        instance = prop.GetValue(null);
                        registerMethod = instance?.GetType().GetMethods()
                            .FirstOrDefault(m => string.Equals(m.Name, "RegisterTransformation", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(m.Name, "Register", StringComparison.OrdinalIgnoreCase));
                    }

                    if (registerMethod == null) return;
                    // If we have an instance and method now, proceed below using that instance
                    try
                    {
                        // Common RegisterTransformation signature: RegisterTransformation(string pattern, string id, string filePath)
                        // We'll attempt likely overloads using reflection; swallow any exceptions.
                        var pattern = "index.html";
                        var id = Guid.NewGuid().ToString();
                        var filePath = $"/web/plugins/Watch Planner Section_0.0.0.4/watchplanner/plugin-client.js";
                        registerMethod.Invoke(instance, new object[] { pattern, id, filePath });
                        return;
                    }
                    catch { /* ignore and continue to other attempts */ }
                }
                else
                {
                    // Try calling registerMethod as static or with null instance (if static), trying common argument shapes
                    var argsVariants = new object[][]
                    {
                        // pattern, id, filePath
                        new object[] { "index.html", Guid.NewGuid().ToString(), $"/web/plugins/Watch Planner Section_0.0.0.4/watchplanner/plugin-client.js" },

                        // pattern, filePath
                        new object[] { "index.html", $"/web/plugins/Watch Planner Section_0.0.0.4/watchplanner/plugin-client.js" },

                        // filePath only
                        new object[] { $"/web/plugins/Watch Planner Section_0.0.0.4/watchplanner/plugin-client.js" }
                    };

                    foreach (var args in argsVariants)
                    {
                        try
                        {
                            // If method is static, provide null instance; otherwise try null (some implementations accept static call)
                            registerMethod.Invoke(null, args);
                            return;
                        }
                        catch
                        {
                            // Try to find an instance-based approach next
                            try
                            {
                                // Try to obtain a service instance via a common static property
                                var instProp = serviceType.GetProperty("Instance") ?? serviceType.GetProperty("Service");
                                var inst = instProp?.GetValue(null);
                                if (inst != null)
                                {
                                    registerMethod.Invoke(inst, args);
                                    return;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch
            {
                // Non-fatal: registration is optional; just swallow exceptions
            }
        }
    }
}
