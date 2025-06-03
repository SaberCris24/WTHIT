using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace Plantilla.Services
{
    /// <summary>
    /// Interface for virus scanning operations
    /// </summary>
    public interface IVirusScanService
    {
        /// <summary>
        /// Gets basic scan report from VirusTotal
        /// </summary>
        Task<string> GetVirusTotalReportAsync(string filePath);

        /// <summary>
        /// Gets detailed file information from VirusTotal
        /// </summary>
        Task<string> GetVirusTotalDetailsAsync(string filePath);
    }

    /// <summary>
    /// Service for scanning files using VirusTotal API
    /// </summary>
    public class VirusScanService : IVirusScanService
    {
        // API key and HTTP client for VirusTotal
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Creates new instance with VirusTotal API key
        /// </summary>
        public VirusScanService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-apikey", _apiKey);
        }

        /// <summary>
        /// Gets basic malware scan report
        /// </summary>
        public async Task<string> GetVirusTotalReportAsync(string filePath)
        {
            try
            {
                // Calculate file hash and get report
                var hash = await GetFileHashAsync(filePath);
                var url = $"https://www.virustotal.com/api/v3/files/{hash}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return "Not found on VT";

                // Parse response and get statistics
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var stats = doc.RootElement
                    .GetProperty("data")
                    .GetProperty("attributes")
                    .GetProperty("last_analysis_stats");

                int malicious = stats.GetProperty("malicious").GetInt32();
                int undetected = stats.GetProperty("undetected").GetInt32();

                return $"Malicious: {malicious}, Undetected: {undetected}";
            }
            catch (Exception ex)
            {
                return $"VT Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets detailed file analysis information
        /// </summary>
        public async Task<string> GetVirusTotalDetailsAsync(string filePath)
        {
            try
            {
                // Get file details from VirusTotal
                var hash = await GetFileHashAsync(filePath);
                var url = $"https://www.virustotal.com/api/v3/files/{hash}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return "No VirusTotal info found.";

                // Extract file information
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var attr = doc.RootElement.GetProperty("data").GetProperty("attributes");

                // Get various file properties
                string? name = attr.TryGetProperty("meaningful_name", out var n) ? n.GetString() : null;
                string? type = attr.TryGetProperty("type_description", out var t) ? t.GetString() : null;
                string? tags = attr.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", tagsProp.EnumerateArray().Select(x => x.GetString() ?? string.Empty))
                    : null;
                string? altName = attr.TryGetProperty("names", out var namesProp) && namesProp.ValueKind == JsonValueKind.Array && namesProp.GetArrayLength() > 0
                    ? namesProp[0].GetString()
                    : null;

                return $"Name: {name ?? "N/A"}\nType: {type ?? "N/A"}\nTags: {tags ?? "N/A"}";
            }
            catch (Exception ex)
            {
                return $"VT Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Calculates SHA256 hash of a file
        /// </summary>
        private async Task<string> GetFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}