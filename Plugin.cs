using System;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Plugin.WatchPlannerSection
{
    public class Plugin : BasePlugin
    {
        public override string Name => "Watch Planner Section";
        public override string Description => "Global watch planner UI injected on home; admin-editable, persisted in plugin config directory.";
        public override Guid Id => Guid.Parse("7b5f2a58-4c2b-4f2f-9f5a-0a6a9f1a9b20");

        // Jellyfin 10.11+ expects a parameterless constructor
        public Plugin() { }
    }
}
