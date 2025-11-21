// src/app-core.js
// Full client UI logic for WatchPlanner plugin
// - Creates the week grid UI under #watchplanner-root
// - Provides admin-only "add" via search modal and a config modal
// - Persists presets using loadPreset / persistPreset from app-api.js
// - Uses minimal Jellyfin Items search via searchSeries from app-api.js

import { loadPreset, persistPreset, searchSeries, itemThumbUrl, itemDetailsLink } from './app-api';

// Config / constants
const DAYS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
const ROOT_ID = 'watchplanner-root';
const STORAGE_MODE_KEY = 'watchplanner.serverSideMode'; // 'server' or 'local'

// Publicly callable apply preset hook (used by app-init hydrate)
window.WatchPlannerApplyPreset = applyPresetToGrid;

// ----------------- Helpers -----------------

function el(tag, attrs = {}, children = []) {
  const node = document.createElement(tag);
  for (const k of Object.keys(attrs)) {
    if (k === 'class') node.className = attrs[k];
    else if (k === 'style') node.style.cssText = attrs[k];
    else if (k === 'dataset') {
      for (const d of Object.keys(attrs[k])) node.dataset[d] = attrs[k][d];
    } else if (k.startsWith('on') && typeof attrs[k] === 'function') {
      node.addEventListener(k.substring(2).toLowerCase(), attrs[k]);
    } else if (k === 'html') {
      node.innerHTML = attrs[k];
    } else {
      node.setAttribute(k, attrs[k]);
    }
  }
  (Array.isArray(children) ? children : [children]).forEach(c => {
    if (c == null) return;
    node.appendChild(typeof c === 'string' ? document.createTextNode(c) : c);
  });
  return node;
}

async function isCurrentUserAdmin() {
  try {
    const resp = await fetch('/Users/Me', { credentials: 'include' });
    if (!resp.ok) return false;
    const j = await resp.json().catch(() => null);
    return !!(j && (j.Policy && j.Policy.IsAdministrator || j.IsAdministrator));
  } catch (e) {
    return false;
  }
}

function getServerSideMode() {
  const v = localStorage.getItem(STORAGE_MODE_KEY);
  return v === 'local' ? 'local' : 'server';
}

function setServerSideMode(mode) {
  localStorage.setItem(STORAGE_MODE_KEY, mode === 'local' ? 'local' : 'server');
}

// ----------------- DOM builders -----------------

function createCard(item) {
  // item: { Id, Name, Type, ServerId, ThumbUrl }
  const link = itemDetailsLink(item.Id, item.ServerId);
  const thumb = item.ThumbUrl || itemThumbUrl(item.Id, item.ServerId, null);
  const card = el('div', { class: 'wp-card card overflowBackdropCard card-hoverable card-withuserdata', dataset: { id: item.Id } }, [
    el('div', { class: 'cardBox cardBox-bottompadded watchplanner-card' }, [
      el('div', { class: 'cardScalable', style: 'position: relative; width: 100%;' }, [
        el('div', { class: 'cardPadder cardPadder-overflowBackdrop lazy-hidden-children', style: 'padding-bottom:56.25%;' }),
        el('a', {
          class: 'cardImageContainer cardContent itemAction lazy',
          href: link,
          'aria-label': item.Name,
          style: `background-image: url("${thumb}"); background-size: cover; background-position: center center; position:absolute; top:0; left:0; width:100%; height:100%;`
        })
      ]),
      el('div', { class: 'cardText cardTextCentered cardText-first' }, [
        el('bdi', {}, [
          el('a', { href: link, class: 'itemAction textActionButton', title: item.Name }, item.Name)
        ])
      ])
    ])
  ]);
  return card;
}

function createDayColumn(day) {
  const header = el('div', { class: 'watchplanner-header-cell', dataset: { day }, style: 'cursor:pointer; background-color: rgba(32,32,32,0.306);' }, day);
  const content = el('div', { class: 'watchplanner-content-cell', dataset: { day } }, []);
  const col = el('div', { class: 'day-column', dataset: { day } }, [header, content]);

  // header click handled later based on admin permission (open search modal)
  return { col, header, content };
}

function createGridContainer() {
  const container = el('div', { class: 'watchplanner-scroll-container', style: 'overflow-x:auto; width:100%;' });
  const inner = el('div', { id: 'watchPlannerSection', class: 'watchplanner-section', style: 'background-color: transparent; border: none;' });
  container.appendChild(inner);
  return { container, inner };
}

// ----------------- Modals -----------------

function createModalShell(id) {
  const overlay = el('div', { class: 'wp-modal-overlay', id, style: 'position:fixed; inset:0; display:flex; align-items:center; justify-content:center; z-index:5000;' });
  const panel = el('div', { class: 'wp-modal-panel', style: 'background:#1e1e1e; padding:16px; border-radius:6px; width: 720px; max-width: 95%; max-height: 90%; overflow:auto; color: #eee;' });
  overlay.appendChild(panel);
  overlay.addEventListener('click', (e) => {
    if (e.target === overlay) overlay.remove();
  });
  return { overlay, panel };
}

function openSearchModal(onSelect) {
  const { overlay, panel } = createModalShell('wp-search-modal');
  const input = el('input', { type: 'search', placeholder: 'Search series by name...', style: 'width:100%; padding:8px; margin-bottom:8px; background:#111; color:#fff; border:1px solid #333;' });
  const results = el('div', { style: 'display:flex; flex-wrap:wrap; gap:8px;' });
  const info = el('div', { style: 'margin-top:8px;color:#bbb;font-size:0.9em' }, 'Type then press Enter or click a result.');

  input.addEventListener('keydown', async (e) => {
    if (e.key === 'Enter') {
      const q = input.value.trim();
      results.innerHTML = 'Searching...';
      const items = await searchSeries(q, 24);
      renderSearchResults(items, results, onSelect);
    }
  });

  panel.appendChild(input);
  panel.appendChild(results);
  panel.appendChild(info);
  document.body.appendChild(overlay);
  input.focus();
}

function renderSearchResults(items, container, onSelect) {
  container.innerHTML = '';
  if (!items || items.length === 0) {
    container.appendChild(el('div', { style: 'color:#ccc' }, 'No results.'));
    return;
  }
  items.forEach(it => {
    const thumb = it.ThumbUrl || itemThumbUrl(it.Id, it.ServerId, null);
    const card = el('div', { class: 'wp-search-item', style: 'width:140px; cursor:pointer; color:#fff;' }, [
      el('div', { style: `width:140px; height:78px; background-image:url("${thumb}"); background-size:cover; background-position:center; border-radius:4px;` }),
      el('div', { style: 'font-size:0.9em; margin-top:6px; white-space:nowrap; overflow:hidden; text-overflow:ellipsis;' }, it.Name)
    ]);
    card.addEventListener('click', () => {
      // remove modal
      const modal = document.querySelector('#wp-search-modal');
      if (modal) modal.remove();
      onSelect(it);
    });
    container.appendChild(card);
  });
}

// ----------------- Grid state management -----------------

// Internal in-memory model, shape:
// { serverWeekGrid: { Mon: [itemObj,...], Tue: [...], ... }, otherSettings: {} }
let model = { serverWeekGrid: {} };

function ensureModelDays() {
  if (!model.serverWeekGrid) model.serverWeekGrid = {};
  for (const d of DAYS) {
    if (!Array.isArray(model.serverWeekGrid[d])) model.serverWeekGrid[d] = [];
  }
}

function applyPresetToGrid(preset) {
  if (!preset || typeof preset !== 'object') return;
  model = preset;
  ensureModelDays();
  renderGridFromModel();
}

function addItemToDay(day, item) {
  if (!model.serverWeekGrid) model.serverWeekGrid = {};
  if (!Array.isArray(model.serverWeekGrid[day])) model.serverWeekGrid[day] = [];
  // avoid duplicates by item Id
  if (model.serverWeekGrid[day].some(x => x.Id === item.Id)) return;
  model.serverWeekGrid[day].push(item);
  renderDay(day);
}

// Remove item helper (by id)
function removeItemFromDay(day, itemId) {
  if (!model.serverWeekGrid || !Array.isArray(model.serverWeekGrid[day])) return;
  model.serverWeekGrid[day] = model.serverWeekGrid[day].filter(x => x.Id !== itemId);
  renderDay(day);
}

// ----------------- Rendering -----------------

function renderDay(day) {
  const content = document.querySelector(`.watchplanner-content-cell[data-day="${day}"]`);
  if (!content) return;
  content.innerHTML = '';
  const items = (model.serverWeekGrid && model.serverWeekGrid[day]) ? model.serverWeekGrid[day] : [];
  const frag = document.createDocumentFragment();
  items.forEach(it => {
    const card = createCard(it);
    // admin remove button
    const removeBtn = el('button', { class: 'wp-remove-btn', style: 'margin-top:6px;background:#222;color:#fff;border:1px solid #444;padding:4px;border-radius:4px;cursor:pointer;' }, 'Remove');
    removeBtn.addEventListener('click', async (ev) => {
      ev.stopPropagation();
      // remove from model and persist
      removeItemFromDay(day, it.Id);
      await tryPersistModel();
    });
    const wrapper = el('div', { style: 'display:inline-block; margin:6px; vertical-align:top; width:140px;' }, [card, removeBtn]);
    frag.appendChild(wrapper);
  });
  content.appendChild(frag);
}

function renderGridFromModel() {
  for (const day of DAYS) renderDay(day);
}

// ----------------- Persistence helpers -----------------

async function tryPersistModel() {
  const mode = getServerSideMode(); // 'server' or 'local'
  const res = await persistPreset(mode, model);
  if (!res || !res.ok) {
    console.warn('WatchPlanner: persist failed', res);
  }
  return res;
}

// ----------------- Wiring / initialization -----------------

async function buildAndMount(root) {
  // Build container
  const { container, inner } = createGridContainer();
  root.appendChild(container);

  // Create header and day columns
  const headerBar = el('div', { style: 'display:flex; gap:8px; align-items:center; margin-bottom:8px;' }, []);
  const configBtn = el('button', { class: 'wp-config-btn', title: 'WatchPlanner Settings', style: 'margin-left:auto;background:#222;color:#fff;border:1px solid #444;padding:6px;border-radius:4px;cursor:pointer;' }, '⚙');
  headerBar.appendChild(configBtn);
  inner.appendChild(headerBar);

  const grid = el('div', { class: 'wp-grid', style: 'display:flex; gap:8px;' });
  inner.appendChild(grid);

  const dayCols = {};
  for (const d of DAYS) {
    const { col, header, content } = createDayColumn(d);
    dayCols[d] = { col, header, content };
    grid.appendChild(col);
  }

  // Add config button behavior
  configBtn.addEventListener('click', () => {
    openConfigModal();
  });

  // Admin-only: clicking header opens search modal to add a series
  const admin = await isCurrentUserAdmin();
  for (const d of DAYS) {
    const headerEl = dayCols[d].header;
    if (admin) {
      headerEl.addEventListener('click', () => {
        openSearchModal(async (selected) => {
          // selected is compact item from searchSeries
          const itemObj = {
            Id: selected.Id,
            Name: selected.Name,
            Type: selected.Type,
            ServerId: selected.ServerId || null,
            ThumbUrl: selected.ThumbUrl || itemThumbUrl(selected.Id, selected.ServerId, null)
          };
          addItemToDay(d, itemObj);
          await tryPersistModel();
        });
      });
      headerEl.style.cursor = 'pointer';
    } else {
      headerEl.style.cursor = 'default';
    }
  }

  // If there were existing nodes inside root (e.g., from injector), keep them
  // Load initial preset from server/local and render
  const mode = getServerSideMode();
  const preset = await loadPreset(mode);
  if (preset) {
    applyPresetToGrid(preset);
  } else {
    ensureModelDays();
    renderGridFromModel();
  }
}

// Basic config modal (allows switching server/local mode and a reset)
function openConfigModal() {
  const { overlay, panel } = createModalShell('wp-config-modal');

  const title = el('div', { style: 'font-weight:700; font-size:1.1em; margin-bottom:8px;' }, 'WatchPlanner Settings');
  panel.appendChild(title);

  const modeLabel = el('div', { style: 'margin-top:8px;' }, 'Persistence mode:');
  const modeSelect = el('select', { style: 'margin-left:8px;padding:6px;background:#111;color:#fff;border:1px solid #333;' }, [
    el('option', { value: 'server' }, 'Server (global)'),
    el('option', { value: 'local' }, 'Local (per user)')
  ]);
  modeSelect.value = getServerSideMode();

  panel.appendChild(modeLabel);
  panel.appendChild(modeSelect);

  const saveBtn = el('button', { style: 'margin-top:12px;background:#2a7;background-color:#2a7;color:#000;padding:8px;border-radius:4px;cursor:pointer;' }, 'Save Settings');
  saveBtn.addEventListener('click', () => {
    const val = modeSelect.value === 'local' ? 'local' : 'server';
    setServerSideMode(val);
    overlay.remove();
  });
  panel.appendChild(saveBtn);

  // Reset grid button (admin only)
  const resetBtn = el('button', { style: 'margin-top:12px;margin-left:8px;background:#aa2;color:#fff;padding:8px;border-radius:4px;cursor:pointer;' }, 'Reset Grid');
  resetBtn.addEventListener('click', async () => {
    model = { serverWeekGrid: {} };
    ensureModelDays();
    renderGridFromModel();
    await tryPersistModel();
    overlay.remove();
  });
  panel.appendChild(resetBtn);

  document.body.appendChild(overlay);
}

// ----------------- Public initializer -----------------

let initialized = false;

export function initializeWatchPlanner() {
  try {
    if (initialized) return;
    initialized = true;
    const root = document.querySelector(`#${ROOT_ID}`);
    if (!root) {
      console.error('WatchPlanner: root element not found:', `#${ROOT_ID}`);
      return;
    }
    // mount UI into root
    buildAndMount(root).catch(err => {
      console.error('WatchPlanner: buildAndMount failed', err);
    });
  } catch (err) {
    console.error('WatchPlanner init error', err);
  }
}

// Expose for testing/debugging
export function getModel() {
  return model;
}
