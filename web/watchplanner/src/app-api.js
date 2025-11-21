// src/app-api.js
// WatchPlanner client API helpers
// - server persistence (GET/POST /plugins/watchplanner/preset)
// - local persistence fallback
// - minimal Jellyfin search helpers used by the "add series" modal

const WATCHPLANNER_PRESET_URL = '/plugins/watchplanner/preset';
const LOCAL_KEY = 'watchplanner.local.preset';

// --- Persistence: server side endpoints ---

export async function fetchAdminPreset() {
  try {
    const resp = await fetch(WATCHPLANNER_PRESET_URL, { credentials: 'include' });
    if (!resp.ok) {
      console.warn('fetchAdminPreset: server returned', resp.status);
      return null;
    }
    const json = await resp.json().catch(() => null);
    return json;
  } catch (err) {
    console.warn('fetchAdminPreset: network error', err);
    return null;
  }
}

export async function saveAdminPreset(presetObj) {
  try {
    const resp = await fetch(WATCHPLANNER_PRESET_URL, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(presetObj)
    });
    if (!resp.ok) {
      const text = await resp.text().catch(() => null);
      console.warn('saveAdminPreset: server error', resp.status, text);
      return { ok: false, status: resp.status, message: text };
    }
    const json = await resp.json().catch(() => ({ status: 'ok' }));
    return { ok: true, data: json };
  } catch (err) {
    console.warn('saveAdminPreset: network error', err);
    return { ok: false, error: String(err) };
  }
}

// --- Local fallback persistence ---

export function loadLocalPreset() {
  try {
    const s = localStorage.getItem(LOCAL_KEY);
    return s ? JSON.parse(s) : null;
  } catch (e) {
    console.warn('loadLocalPreset parse error', e);
    return null;
  }
}

export function saveLocalPreset(obj) {
  try {
    localStorage.setItem(LOCAL_KEY, JSON.stringify(obj));
    return true;
  } catch (e) {
    console.warn('saveLocalPreset failed', e);
    return false;
  }
}

// --- Convenience loaders/savers used by app-core ---

// mode: 'server' or 'local'
export async function loadPreset(mode = 'server') {
  if (mode === 'server') {
    const server = await fetchAdminPreset();
    if (server) return server;
    return loadLocalPreset();
  }
  return loadLocalPreset();
}

export async function persistPreset(mode = 'server', presetObj) {
  if (mode === 'server') {
    const res = await saveAdminPreset(presetObj);
    if (res.ok) return res;
    // fallback to local
    saveLocalPreset(presetObj);
    return { ok: true, fallback: true };
  } else {
    saveLocalPreset(presetObj);
    return { ok: true };
  }
}

// --- Jellyfin minimal search helpers ---
// These use the public Jellyfin HTTP API routes relative to the served client.
// We only search for Series and return a compact result shape for the UI.

function buildUrl(path, params = {}) {
  const u = new URL(path, window.location.origin);
  Object.keys(params).forEach(k => {
    if (params[k] !== undefined && params[k] !== null) u.searchParams.set(k, params[k]);
  });
  return u.toString();
}

// Search series by name (simple). Returns array of { Id, Name, Type, ServerId, ThumbUrl }
export async function searchSeries(query, limit = 12) {
  if (!query || query.trim().length === 0) return [];
  try {
    // Use Items endpoint with searchTerm and filter to Series
    // Adjust query params as needed for your Jellyfin version
    const url = buildUrl('/Items', {
      SearchTerm: query,
      IncludeItemTypes: 'Series',
      Recursive: true,
      Limit: limit
    });
    const resp = await fetch(url, { credentials: 'include' });
    if (!resp.ok) {
      console.warn('searchSeries: server responded', resp.status);
      return [];
    }
    const j = await resp.json().catch(() => null);
    if (!j || !Array.isArray(j.Items)) return [];
    // Map to compact shape
    return j.Items.map(it => ({
      Id: it.Id,
      Name: it.Name,
      Type: it.Type,
      ServerId: it.ServerId || null,
      ThumbUrl: itemThumbUrl(it.Id, it.ServerId, it.ImageTags && it.ImageTags.Thumb ? it.ImageTags.Thumb : (it.ImageTags ? Object.values(it.ImageTags)[0] : null))
    }));
  } catch (err) {
    console.warn('searchSeries error', err);
    return [];
  }
}

// Helper to build a thumbnail URL for an item (works with Jellyfin /Items/{id}/Images/Thumb)
export function itemThumbUrl(itemId, serverId = null, thumbTag = null) {
  // If the serverId exists, the client may still use the same origin; build the relative path
  // Use the /jelly prefix if your installation serves assets under /jelly — adjust if needed.
  // We'll favor the same-origin path used in your environment.
  if (!itemId) return null;
  // Use tag when available for cache busting
  const tagPart = thumbTag ? `?tag=${encodeURIComponent(thumbTag)}` : '';
  return `/jelly/Items/${encodeURIComponent(itemId)}/Images/Thumb${tagPart}`;
}

// Helper to build details link used in cards
export function itemDetailsLink(itemId, serverId) {
  if (!itemId) return '#';
  return `#/details?id=${encodeURIComponent(itemId)}${serverId ? '&serverId=' + encodeURIComponent(serverId) : ''}`;
}
