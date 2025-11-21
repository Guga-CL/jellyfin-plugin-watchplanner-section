// plugin-client.js
// Small loader the plugin registers to make sure the WatchPlanner mount runs on client load.

(function(){
  const rootId = 'watchplanner-root';
  if (!document.querySelector('#' + rootId)) {
    const homeContainer = document.querySelector('div.homeSectionsContainer') || document.body;
    const root = document.createElement('div');
    root.id = rootId;
    root.className = 'watchplanner-root';
    homeContainer.appendChild(root);
  }

  if (typeof window.WatchPlannerInit === 'function') {
    try { window.WatchPlannerInit(document.querySelector('#' + rootId)); }
    catch(e){ console.error('WatchPlannerInit failed', e); }
  } else {
    window.dispatchEvent(new CustomEvent('watchplanner:ready', { detail: { root: document.querySelector('#' + rootId) } }));
  }
})();
