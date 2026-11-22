using System;
using System.IO;

namespace WTHIT.Core
{
    /// <summary>
    /// Centralized configuration management for application settings and API keys
    /// </summary>
    public static class Configuration
    {
        internal static string LocalDataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WTHIT");

        private static readonly string VirusTotalApiKeyKey = "VirusTotalApiKey";
        private static readonly string EnableAutoRefreshKey = "EnableAutoRefresh";
        private static readonly string EnableCompactModeKey = "EnableCompactMode";
        private static readonly string MaxScanResultsKey = "MaxScanResults";

        /// <summary>Gets or persists the VirusTotal API key.</summary>
        public static string VirusTotalApiKey
        {
            get
            {
                try
                {
                    return SettingsManager.Instance.GetValue(VirusTotalApiKeyKey, string.Empty);
                }
                catch
                {
                    return string.Empty;
                }
            }
            set
            {
                var apiKey = value.Trim();
                if (apiKey.Contains('\r') || apiKey.Contains('\n') || apiKey.Contains('\0'))
                    throw new ArgumentException("The API key must be a single line.");

                SettingsManager.Instance.SetValueOrThrow(VirusTotalApiKeyKey, apiKey);
            }
        }

        public static bool EnableAutoRefresh
        {
            get
            {
                try
                {
                    return SettingsManager.Instance.GetValue(EnableAutoRefreshKey, true);
                }
                catch
                {
                    return true;
                }
            }
            set
            {
                try
                {
                    SettingsManager.Instance.SetValue(EnableAutoRefreshKey, value);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save auto-refresh setting: {ex.Message}");
                }
            }
        }

        public static bool EnableCompactMode
        {
            get
            {
                try
                {
                    return SettingsManager.Instance.GetValue(EnableCompactModeKey, false);
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                try
                {
                    SettingsManager.Instance.SetValue(EnableCompactModeKey, value);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save compact mode setting: {ex.Message}");
                }
            }
        }

        public static int MaxScanResults
        {
            get
            {
                try
                {
                    return Math.Clamp(SettingsManager.Instance.GetValue(MaxScanResultsKey, DefaultMaxScanCount),
                        MinScanResults, MaxScanResultsLimit);
                }
                catch
                {
                    return DefaultMaxScanCount;
                }
            }
            set
            {
                try
                {
                    var clampedValue = Math.Clamp(value, MinScanResults, MaxScanResultsLimit);
                    SettingsManager.Instance.SetValue(MaxScanResultsKey, clampedValue);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save max scan results: {ex.Message}");
                }
            }
        }

        public static bool IsVirusTotalConfigured => !string.IsNullOrEmpty(VirusTotalApiKey);
        public const int DefaultMaxScanCount = 10;
        public const int MinScanResults = 5;
        public const int MaxScanResultsLimit = 50;
        public const int DefaultMaxPoolSize = 200;
        public const int DefaultRefreshIntervalSeconds = 5;

        public static void ResetAllSettings()
        {
            try
            {
                SettingsManager.Instance.Remove(VirusTotalApiKeyKey);
                SettingsManager.Instance.Remove(EnableAutoRefreshKey);
                SettingsManager.Instance.Remove(EnableCompactModeKey);
                SettingsManager.Instance.Remove(MaxScanResultsKey);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to reset settings: {ex.Message}");
            }
        }
    }
}
