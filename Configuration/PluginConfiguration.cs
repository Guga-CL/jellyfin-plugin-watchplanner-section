using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.WatchPlannerSection.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string ExampleSetting { get; set; } = "default";
    }
}
