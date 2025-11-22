using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.WatchPlannerSection.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string ExampleSetting { get; set; } = "default";

        public PluginConfiguration()
        {
            // Only set defaults here, no filesystem or Jellyfin calls
        }
    }
}
