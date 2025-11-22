using System;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Jellyfin.Plugin.WatchPlannerSection.Configuration;

namespace Jellyfin.Plugin.WatchPlannerSection
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public override string Name => "Watch Planner Section";
        public override string Description => "Global watch planner UI injected on home; admin-editable, persisted in plugin config directory.";
        public override Guid Id => Guid.Parse("631fa65f-0afb-4933-8026-cf41eefc6444");

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            // No additional logic here
        }
    }
}
