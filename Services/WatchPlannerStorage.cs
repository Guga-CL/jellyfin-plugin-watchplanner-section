using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.WatchPlannerSection.Services
{
    public class WatchPlannerStorage
    {
        private readonly IApplicationPaths _appPaths;
        private readonly string _filePath;

        public WatchPlannerStorage(IApplicationPaths appPaths)
        {
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            var configDir = Path.Combine(_appPaths.PluginConfigurationsPath, "WatchPlannerSection");
            Directory.CreateDirectory(configDir);
            _filePath = Path.Combine(configDir, "watchplanner-config.json");
        }

        public async Task<string> ReadAsync()
        {
            try
            {
                if (!File.Exists(_filePath)) return "{}";
                using var fs = File.Open(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var sr = new StreamReader(fs, Encoding.UTF8);
                return await sr.ReadToEndAsync().ConfigureAwait(false);
            }
            catch
            {
                return "{}";
            }
        }

        public async Task WriteAsync(string json)
        {
            if (json is null) throw new ArgumentNullException(nameof(json));
            var tmp = _filePath + ".tmp";
            try
            {
                using (var fs = File.Open(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    await sw.WriteAsync(json).ConfigureAwait(false);
                    await sw.FlushAsync().ConfigureAwait(false);
                }

                File.Replace(tmp, _filePath, null);
            }
            catch
            {
                try { File.WriteAllText(_filePath, json, Encoding.UTF8); } catch { }
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }
    }
}
