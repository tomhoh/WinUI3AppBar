using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace AppAppBar3
{
    // Cross-mode settings store: a JSON file in %LOCALAPPDATA%\AppAppBar3\settings.json.
    // Works identically for packaged (MSIX) and unpackaged builds — Windows.Storage.ApplicationData
    // throws in fully unpackaged apps, so we can't rely on it if we want a portable .exe.
    public static class SettingMethods
    {
        private static readonly string SettingsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppAppBar3");
        private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

        private static Dictionary<string, object> _cache;

        private static Dictionary<string, object> Cache
        {
            get
            {
                if (_cache != null) return _cache;
                _cache = Load();
                return _cache;
            }
        }

        private static Dictionary<string, object> Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new Dictionary<string, object>();
                var json = File.ReadAllText(SettingsPath);
                if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>();
                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                          ?? new Dictionary<string, JsonElement>();
                var result = new Dictionary<string, object>(raw.Count);
                foreach (var kv in raw) result[kv.Key] = ConvertElement(kv.Value);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to load settings.json: " + ex.Message);
                return new Dictionary<string, object>();
            }
        }

        private static object ConvertElement(JsonElement el) => el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt32(out int i) ? (object)i : el.GetDouble(),
            JsonValueKind.True   => true,
            JsonValueKind.False  => false,
            JsonValueKind.Null   => null,
            _                    => null,
        };

        public static void setDefaultValues()
        {
            saveSetting("bar_size", 50);
            saveSetting("monitor", @"\\.\DISPLAY1");
            saveSetting("LoadOnStartup", true);
            saveSetting("edge", 1);
            saveSetting("autohide", false);
            saveSetting("theme", "Default");
        }

        public static void saveSetting(string setting, object value)
        {
            Cache[setting] = value;
            Flush();
        }

        public static object loadSettings(string setting)
        {
            return Cache.TryGetValue(setting, out var v) ? v : null;
        }

        private static void Flush()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                File.WriteAllText(SettingsPath,
                    JsonSerializer.Serialize(Cache, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to persist settings.json: " + ex.Message);
            }
        }

        // True when running inside an MSIX package (has Package identity).
        // Used to pick StartupTask API vs. HKCU Run key for "load on startup".
        public static bool IsPackaged()
        {
            try { _ = Windows.ApplicationModel.Package.Current; return true; }
            catch { return false; }
        }
    }
}
