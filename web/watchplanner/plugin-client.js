(function () {
  console.log("watchplanner-injected")
  // Detect if we are on Jellyfin home page
  function isHome() {
    return location.hash.startsWith('#/home') || location.hash === '' || location.hash === '#';
  }

  // Wait for home DOM to be ready
  function waitForHomeSection(maxTries = 60) {
    return new Promise((resolve) => {
      const iv = setInterval(() => {
        const container = document.querySelector('.homeSection, .dashboardSection, #app');
        if (container) {
          clearInterval(iv);
          resolve(container);
        }
        if (--maxTries <= 0) {
          clearInterval(iv);
          resolve(null);
        }
      }, 250);
    });
  }

  // Admin check via ApiClient
  async function isAdmin(api) {
    try {
      const uid = api.getCurrentUserId();
      const user = await api.getUser(uid);
      return user?.Policy?.IsAdministrator === true;
    } catch {
      return false;
    }
  }

  // GET config from backend
  async function getConfig() {
    try {
      const res = await fetch('/watchplanner/config', { credentials: 'include' });
      if (!res.ok) throw new Error('Failed to load config');
      const data = await res.json();
      return data?.schedule || {};
    } catch {
      return {};
    }
  }

  // POST config (admins only)
  async function saveConfig(schedule) {
    const body = JSON.stringify({ schedule });
    const res = await fetch('/watchplanner/config', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body
    });
    if (!res.ok) throw new Error('Failed to save config');
  }

  // Search series via Jellyfin API
  async function searchSeries(api, term) {
    const uid = api.getCurrentUserId();
    const p = {
      SearchTerm: term,
      IncludeItemTypes: 'Series',
      Limit: 12,
      Recursive: true
    };
    const res = await api.getItems(uid, p);
    const items = res?.Items || [];
    return items.map(it => ({
      id: it.Id,
      name: it.Name,
      img: api.getImageUrl(it.Id, { type: 'Primary', maxHeight: 120 }) || ''
    }));
  }

  // Build root container
  function buildRoot() {
    const root = document.createElement('div');
    root.id = 'watchplanner-root';
    root.className = 'watchplanner';
    root.innerHTML = `
      <div class="wp-header">
        <span class="wp-title">Watch Planner</span>
        <button class="wp-config-btn" style="display:none">Config</button>
      </div>
      <div class="wp-days">
        ${['Mon','Tue','Wed','Thu','Fri','Sat','Sun'].map(d => `
          <div class="wp-day">
            <div class="watchplanner-header-cell">${d}</div>
            <div class="wp-day-list" data-day="${d}"></div>
          </div>`).join('')}
      </div>
      <div class="wp-modal-overlay" style="display:none">
        <div class="wp-modal">
          <div class="wp-modal-header">
            <span>Search series</span>
            <button class="wp-close">×</button>
          </div>
          <div class="wp-modal-body">
            <input class="wp-search-input" type="text" placeholder="Type series name..." />
            <div class="wp-results"></div>
          </div>
        </div>
      </div>
    `;
    return root;
  }

  function injectStyles() {
    const css = `
      #watchplanner-root { margin: 12px 0; padding: 10px; background: var(--theme-background); border-radius: 8px; }
      .wp-header { display:flex; justify-content:space-between; align-items:center; margin-bottom:8px; }
      .wp-title { font-weight:600; }
      .wp-days { display:grid; grid-template-columns: repeat(7, 1fr); gap: 8px; }
      .wp-day { background: var(--theme-background-alt); border-radius:6px; padding:6px; }
      .watchplanner-header-cell { font-weight:600; margin-bottom:6px; cursor:pointer; }
      .wp-day-list { display:flex; flex-direction:column; gap:6px; min-height: 80px; }
      .wp-item { display:flex; align-items:center; gap:8px; }
      .wp-item img { width:40px; height:60px; object-fit:cover; border-radius:4px; }
      .wp-modal-overlay { position:fixed; inset:0; background:rgba(0,0,0,0.4); display:flex; align-items:center; justify-content:center; z-index:9999; }
      .wp-modal { width:480px; background:var(--theme-background); border-radius:8px; box-shadow:0 4px 22px rgba(0,0,0,0.35); }
      .wp-modal-header { display:flex; justify-content:space-between; align-items:center; padding:10px; border-bottom:1px solid var(--theme-text-invert); }
      .wp-modal-body { padding:10px; }
      .wp-search-input { width:100%; padding:8px; border-radius:6px; border:1px solid var(--theme-text-invert); margin-bottom:8px; }
      .wp-results { display:flex; flex-direction:column; gap:6px; max-height:300px; overflow:auto; }
      .wp-result { display:flex; align-items:center; gap:10px; cursor:pointer; padding:6px; border-radius:6px; }
      .wp-result:hover { background:var(--theme-background-alt); }
      .wp-close { background:none; border:none; font-size:18px; cursor:pointer; }
    `;
    const style = document.createElement('style');
    style.textContent = css;
    document.head.appendChild(style);
  }

  function renderSchedule(root, schedule) {
    root.querySelectorAll('.wp-day-list').forEach(list => {
      const day = list.dataset.day;
      const items = schedule[day] || [];
      list.innerHTML = items.map(s => `
        <div class="wp-item">
          <img src="${s.img}" alt="">
          <div class="wp-meta">
            <div class="wp-name">${s.name}</div>
          </div>
        </div>`).join('');
    });
  }

  function openModal(root) {
    root.querySelector('.wp-modal-overlay').style.display = 'flex';
    root.querySelector('.wp-search-input').value = '';
    root.querySelector('.wp-results').innerHTML = '';
    root.querySelector('.wp-search-input').focus();
  }

  function closeModal(root) {
    root.querySelector('.wp-modal-overlay').style.display = 'none';
  }

  async function bootstrap() {
    if (!isHome()) return;
    const container = await waitForHomeSection();
    if (!container || !window.ApiClient) return;

    injectStyles();
    const root = buildRoot();
    container.prepend(root);

    const api = window.ApiClient;
    const admin = await isAdmin(api);
    let schedule = await getConfig();
    renderSchedule(root, schedule);

    if (admin) {
      root.querySelector('.wp-config-btn').style.display = 'inline-flex';

      const overlay = root.querySelector('.wp-modal-overlay');
      const closeBtn = root.querySelector('.wp-close');
      const input = root.querySelector('.wp-search-input');
      const results = root.querySelector('.wp-results');

      closeBtn.addEventListener('click', () => closeModal(root));
      overlay.addEventListener('click', (e) => {
        if (e.target === overlay) closeModal(root);
      });

      root.querySelectorAll('.watchplanner-header-cell').forEach(cell => {
        cell.addEventListener('click', () => {
          root.dataset.activeDay = cell.textContent.trim();
          openModal(root);
        });
      });

      let searchTimer = null;
      input.addEventListener('input', () => {
        const term = input.value.trim();
        results.innerHTML = '';
        if (searchTimer) clearTimeout(searchTimer);
        if (!term) return;
        searchTimer = setTimeout(async () => {
          const items = await searchSeries(api, term);
          results.innerHTML = items.map(it => `
            <div class="wp-result" data-id="${it.id}" data-name="${it.name}" data-img="${it.img}">
              <img src="${it.img}" width="40" height="60" />
              <div>${it.name}</div>
            </div>`).join('');
          results.querySelectorAll('.wp-result').forEach(el => {
            el.addEventListener('click', async () => {
              const day = root.dataset.activeDay;
              if (!day) return;
              schedule[day] = schedule[day] || [];
              schedule[day].push({
                id: el.dataset.id,
                name: el.dataset.name,
                img: el.dataset.img
              });
              renderSchedule(root, schedule);
              closeModal(root);
              try {
                await saveConfig(schedule);
              } catch (e) {
                console.error(e);
                alert('Failed to save config');
              }
            });
          });
        }, 250);
      });
    }
  }

  // Start bootstrap
  bootstrap();
})();

