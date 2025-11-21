# Watch Planner section plugin

A minimal Jellyfin plugin that provides a global “Watch Planner” on the home page.  
All users can view the planner; only admins can edit. Planner data is stored on disk in the plugin’s configuration directory.  
Front‑end is auto‑injected when JavaScript Injector (or compatible) is present; otherwise you can add a small loader later.

Obs.: This is just a test script to understand how to configure a plugin and use jellyfin API, expect to see some odd stuff that may not be useful for anyone. 

---

## What this plugin does

- **Frontend UI:** mounts a “Watch Planner” div on the Jellyfin home page with 7 day columns.
- **Admin actions:** admins can open a modal to search Jellyfin for series, select one, and add it to a day; they can also open a config view if needed.
- **Global persistence:** changes are saved to a single JSON file in the plugin’s configuration directory so every user sees the same schedule.
- **Automatic injection:** on startup, the plugin attempts to register its client script with JavaScript Injector (or File Transformation) so the planner appears without manual edits.

---

## Architecture overview

- **Backend (DLL):**
  - Exposes two endpoints:
    - **GET /watchplanner/config:** returns the current server‑side JSON config.
    - **POST /watchplanner/config:** saves updates to the JSON file (admins only).
  - On startup, tries to register a client script injection with JavaScript Injector/File Transformation if present.

- **Frontend (JS bundle):**
  - Injected into the home page.
  - Renders planner UI for all users.
  - Detects admin status; only admins can open the modal and commit changes.
  - Reads/writes JSON via the plugin endpoints.
  - Uses Jellyfin’s web API to search series and resolve images.

- **Storage:**
  - JSON file is stored in Jellyfin’s plugin configuration directory:
    - On Windows: `%AppData%\Jellyfin\plugins\config\watchplanner-config.json`
    - On Linux: `/var/lib/jellyfin/plugins/config/watchplanner-config.json`
  - This path is provided by `IApplicationPaths.PluginConfigurationsPath` and is guaranteed writable by Jellyfin.

---

## File layout

- **Plugin DLL + meta.json** deployed to:
  - `%LocalAppData%\jellyfin\plugins\Watch Planner Section_0.0.0.1\`
- **Web assets (JS/CSS)** deployed to:
  - `%LocalAppData%\jellyfin\jellyfin-web\plugins\Watch Planner Section_0.0.0.1\watchplanner\`
- **Server config JSON:**
  - `%AppData%\Jellyfin\plugins\config\watchplanner-config.json`

---

## Endpoints

- **GET /watchplanner/config**
  - Returns the planner JSON (e.g., `{ "Mon": [{ id, name, img }], ... }`).
  - Available to all authenticated users.

- **POST /watchplanner/config**
  - Accepts updated JSON in the request body.
  - Requires admin; non‑admins receive 403/Forbid.
  - Overwrites the server‑side JSON file.

---

## Injection behavior

- **On plugin startup:**
  - The plugin attempts to locate JavaScript Injector/File Transformation.
  - If found, it registers a script tag that points to:
    - `/web/plugins/Watch%20Planner%20Section_0.0.0.1/watchplanner/plugin-client.js`
  - The injector adds the script to the home page automatically.

- **If injector is not present:**
  - The front‑end will not auto‑load.
  - Future enhancement: provide an admin “Enable loader” button that copies a tiny loader into the webroot.

---

## Permissions

- **Viewing:** all authenticated users.
- **Editing (add/remove/search/config):** admins only.
- **Persistence:** only admins can POST to the config endpoint.

---

## Development workflow

1. **Build the server:**
   - `dotnet build -c Release`
2. **Build the frontend:**
   - Produce one bundle (`plugin-client.js`; plus `styles.css` if needed).
3. **Deploy:**
   - Copy DLL + meta.json into `%LocalAppData%\jellyfin\plugins\Watch Planner Section_0.0.0.1\`
   - Copy web assets into `%LocalAppData%\jellyfin\jellyfin-web\plugins\Watch Planner Section_0.0.0.1\watchplanner\`
4. **Ensure the config path exists:**
   - `%AppData%\Jellyfin\plugins\config\watchplanner-config.json`
5. **Restart Jellyfin and hard‑refresh the browser.**
6. **Verify in a private window:**
   - `/web/plugins/.../plugin-client.js` loads (DevTools → Network).
   - `GET /watchplanner/config` returns JSON.
   - Admin actions work and `POST` updates the file.

---

## Notes and gotchas

- **Base path:** some setups serve static assets under `/web`. The plugin’s registration uses `/web/plugins/...` to avoid 404s.
- **Writable storage:** `PluginConfigurationsPath` is guaranteed writable by Jellyfin; no symlinks or Program Files hacks needed.
- **Caching:** after updating assets, hard‑refresh or use a new private session.
- **Images:** resolve via Jellyfin ApiClient helpers; adjust for series posters as needed.
- **Future portability:** we can embed web assets inside the DLL and serve them via `IHasWebPages`/embedded resources to avoid external copies if desired.

---

## Goals kept, scope trimmed

- **Kept:** main planner div, 7 day columns, modal search, admin‑only edits, global shared schedule, existing visual style (simplified).
- **Removed:** playback controls, mark‑watched overlay, nonessential UI logic.
- **Plugin purpose:** endpoints + auto‑injection registration; frontend does the rest.
