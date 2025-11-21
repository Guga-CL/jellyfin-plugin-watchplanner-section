// src/app-init.js
// Minimal bootstrap for WatchPlanner that matches the trimmed app-core and app-api

import { initializeWatchPlanner } from './app-core';
import { fetchAdminPreset } from './app-api';

// Expose for debugging if not already present
window.initializeWatchPlanner = window.initializeWatchPlanner || initializeWatchPlanner;

// Deterministic mount wrapper that calls initializeWatchPlanner
(function(){
  function mountInto(root) {
    if (!root) return;
    if (typeof initializeWatchPlanner === 'function') {
      try { initializeWatchPlanner(); }
      catch (err) { console.error('WatchPlanner: initializeWatchPlanner threw', err); }
    } else {
      console.error('WatchPlanner: initializeWatchPlanner not found; ensure app-core loaded');
    }
  }

  // If the root exists already, mount immediately
  const existing = document.querySelector('#watchplanner-root');
  if (existing) mountInto(existing);

  // Stable global used by plugin-client.js
  window.WatchPlannerInit = mountInto;

  // Support event-based mount
  window.addEventListener('watchplanner:ready', function(e){
    const root = e && e.detail && e.detail.root ? e.detail.root : document.querySelector('#watchplanner-root');
    mountInto(root);
  });

  // Try to hydrate preset non-blocking
  (async function tryHydrate() {
    try {
      const preset = await fetchAdminPreset();
      if (preset && typeof window.WatchPlannerApplyPreset === 'function') {
        try { window.WatchPlannerApplyPreset(preset); }
        catch (err) { console.warn('WatchPlanner: apply preset failed', err); }
      }
    } catch (e) {
      // ignore
    }
  })();
})();
