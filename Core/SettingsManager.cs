using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace WTHIT.Core
{
    public sealed class SettingsManager
    {
        private static readonly Lazy<SettingsManager> _instance = new(() => new SettingsManager());
        public static SettingsManager Instance => _instance.Value;

        private readonly string _settingsPath;
        private readonly object _lock = new();

        private SettingsManager()
        {
            // Use the same settings path for packaged and unpackaged builds.
            var appFolder = Configuration.LocalDataDirectory;
            Directory.CreateDirectory(appFolder);
            _settingsPath = Path.Combine(appFolder, "settings.json");
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            try
            {
                lock (_lock)
                {
                    if (!File.Exists(_settingsPath)) return defaultValue;
                    var json = File.ReadAllText(_settingsPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
                    if (dict.TryGetValue(key, out var element))
                    {
                        return JsonSerializer.Deserialize<T>(element.GetRawText()) ?? defaultValue;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings GetValue error: {ex.Message}");
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            try
            {
                SetValueOrThrow(key, value);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings SetValue error: {ex.Message}");
            }
        }

        /// <summary>Persists a setting and reports write failures to the caller.</summary>
        public void SetValueOrThrow<T>(string key, T value)
        {
            lock (_lock)
            {
                var settings = File.Exists(_settingsPath)
                    ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(_settingsPath)) ?? new()
                    : new Dictionary<string, JsonElement>();

                settings[key] = JsonSerializer.SerializeToElement(value);
                var temporaryPath = $"{_settingsPath}.{Guid.NewGuid():N}.tmp";

                try
                {
                    File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings));
                    File.Move(temporaryPath, _settingsPath, overwrite: true);
                }
                finally
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        public void Remove(string key)
        {
            try
            {
                lock (_lock)
                {
                    if (!File.Exists(_settingsPath)) return;
                    var json = File.ReadAllText(_settingsPath);
                    var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? new();
                    if (dict.Remove(key))
                    {
                        var newJson = JsonSerializer.Serialize(dict);
                        File.WriteAllText(_settingsPath, newJson);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Settings Remove error: {ex.Message}");
            }
        }
    }
}
