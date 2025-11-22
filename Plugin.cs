using System;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Plugin.WatchPlannerSection
{
    public class Plugin : BasePlugin
    {
        public override string Name => "Watch Planner Section";
        public override string Description => "Global watch planner UI injected on home; admin-editable, persisted in plugin config directory.";
        public override Guid Id => Guid.Parse("631fa65f-0afb-4933-8026-cf41eefc6444");

        // Jellyfin 10.11+ expects a parameterless constructor
        public Plugin() { }
    }
}
