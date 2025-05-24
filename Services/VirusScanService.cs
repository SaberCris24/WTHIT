using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace Plantilla.Services
{
    public interface IVirusScanService
    {
        Task<string> GetVirusTotalReportAsync(string filePath);
        Task<string> GetVirusTotalDetailsAsync(string filePath);
    }

    public class VirusScanService : IVirusScanService
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public VirusScanService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("x-apikey", _apiKey);
        }

        public async Task<string> GetVirusTotalReportAsync(string filePath)
        {
            try
            {
                var hash = await GetFileHashAsync(filePath);
                var url = $"https://www.virustotal.com/api/v3/files/{hash}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return "Not found on VT";

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

        public async Task<string> GetVirusTotalDetailsAsync(string filePath)
        {
            try
            {
                var hash = await GetFileHashAsync(filePath);
                var url = $"https://www.virustotal.com/api/v3/files/{hash}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return "No VirusTotal info found.";

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var attr = doc.RootElement.GetProperty("data").GetProperty("attributes");

                string name = attr.TryGetProperty("meaningful_name", out var n) ? n.GetString() : null;
                string type = attr.TryGetProperty("type_description", out var t) ? t.GetString() : null;
                string tags = attr.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array
                    ? string.Join(", ", tagsProp.EnumerateArray().Select(x => x.GetString()))
                    : null;
                string altName = attr.TryGetProperty("names", out var namesProp) && namesProp.ValueKind == JsonValueKind.Array && namesProp.GetArrayLength() > 0
                    ? namesProp[0].GetString()
                    : null;

                return $"Name: {name ?? "N/A"}\nType: {type ?? "N/A"}\nTags: {tags ?? "N/A"}";
            }
            catch (Exception ex)
            {
                return $"VT Error: {ex.Message}";
            }
        }

        private async Task<string> GetFileHashAsync(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}