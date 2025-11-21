// src/config/app-config.js
// Minimal configuration shim used by some tooling or legacy imports.
// Adjust values if your Jellyfin uses a non-root base path (e.g., '/jelly').

const config = {
  // Base path where Jellyfin web is hosted. If you access Jellyfin under /jelly, set '/jelly'
  basePath: '/jelly',
  // Name/id used by the plugin; used by some older code paths
  pluginId: 'watchplanner',
  // Toggle debug logs
  debug: false
};

export default config;
