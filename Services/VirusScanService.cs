using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WTHIT.Core;
using WTHIT.Models;

namespace WTHIT.Services
{
    public sealed class VirusScanService : IDisposable
    {
        private static readonly Lazy<VirusScanService> _instance = new(() => new VirusScanService());
        public static VirusScanService Shared => _instance.Value;

        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMinutes(2) };
        private bool _disposed;

        private VirusScanService() { }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Configuration.VirusTotalApiKey);

        /// <summary>Looks up the existing VirusTotal report for the local file's SHA-256 hash.</summary>
        public async Task<VirusTotalScanResult> ScanFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (_disposed) return Unavailable("Service disposed");
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));
            if (!File.Exists(filePath)) return Unavailable("File not found");

            var apiKey = Configuration.VirusTotalApiKey.Trim();
            if (apiKey.Length == 0)
                return Unavailable("VirusTotal API key not configured");

            try
            {
                string hash = await GetFileHashAsync(filePath, cancellationToken);
                string url = $"https://www.virustotal.com/api/v3/files/{hash}";
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-apikey", apiKey);
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return (int)response.StatusCode switch
                    {
                        404 => Unavailable("No report exists for this file on VirusTotal.", hash),
                        401 => Unavailable("VirusTotal rejected the API key.", hash),
                        403 => Unavailable("The API key does not have permission for this request.", hash),
                        429 => Unavailable("VirusTotal request limit reached. Try again later.", hash),
                        _ => Unavailable($"VirusTotal returned HTTP {(int)response.StatusCode}.", hash)
                    };
                }

                string json = await response.Content.ReadAsStringAsync(cancellationToken);
                return ParseVirusTotalResponse(json, hash);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return Unavailable("The VirusTotal request timed out.");
            }
            catch (HttpRequestException)
            {
                return Unavailable("Could not connect to VirusTotal.");
            }
            catch (JsonException)
            {
                return Unavailable("VirusTotal returned an invalid report.");
            }
            catch (FormatException)
            {
                return Unavailable("The VirusTotal API key format is invalid.");
            }
            catch (UnauthorizedAccessException)
            {
                return Unavailable("Windows denied access to the file.");
            }
            catch (IOException)
            {
                return Unavailable("The file could not be read.");
            }
        }

        private static VirusTotalScanResult Unavailable(string message, string? hash = null) =>
            new() { Summary = message, SHA256 = hash, ReportAvailable = false };

        private static async Task<string> GetFileHashAsync(string filePath, CancellationToken cancellationToken)
        {
            await using var stream = File.OpenRead(filePath);
            byte[] hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private static VirusTotalScanResult ParseVirusTotalResponse(string json, string hash)
        {
            using var doc = JsonDocument.Parse(json);

            var attributes = GetProperty(GetProperty(doc.RootElement, "data"), "attributes");
            if (attributes.ValueKind != JsonValueKind.Object)
                return Unavailable("VirusTotal returned an invalid report.", hash);

            var stats = GetProperty(attributes, "last_analysis_stats");
            var engines = ReadEngineResults(attributes);
            int Count(string category) => Math.Max(0, GetInteger(stats, category)
                ?? engines.Count(engine => engine.Category == category));

            var result = new VirusTotalScanResult
            {
                ReportAvailable = true,
                Malicious = Count("malicious"),
                Undetected = Count("undetected"),
                Suspicious = Count("suspicious"),
                Harmless = Count("harmless"),
                Timeout = Count("timeout"),
                ConfirmedTimeout = Count("confirmed-timeout"),
                Failure = Count("failure"),
                TypeUnsupported = Count("type-unsupported"),
                EngineResults = engines.Count == 0 ? null : string.Join("\n", engines.Select(engine =>
                    $"{engine.Name}: {engine.Category ?? "unknown"}{(engine.Result == null ? "" : $" — {engine.Result}")}")),
                MeaningfulName = GetStringSafe(attributes, "meaningful_name"),
                TypeDescription = GetStringSafe(attributes, "type_description"),
                Tags = ExtractTags(attributes),
                Size = GetLong(attributes, "size"),
                FirstSubmissionDate = FormatDate(attributes, "first_submission_date"),
                LastSubmissionDate = FormatDate(attributes, "last_submission_date"),
                LastAnalysisDate = FormatDate(attributes, "last_analysis_date"),
                TimesSubmitted = GetInteger(attributes, "times_submitted") ?? 0,
                Reputation = GetInteger(attributes, "reputation") ?? 0,
                CommunityVotesHarmless = GetInteger(GetProperty(attributes, "total_votes"), "harmless") ?? 0,
                CommunityVotesMalicious = GetInteger(GetProperty(attributes, "total_votes"), "malicious") ?? 0,
                ThreatNames = JoinLines(engines
                    .Where(engine => engine.Category is "malicious" or "suspicious")
                    .Select(engine => $"{engine.Name}: {engine.Result ?? engine.Category}")),
                Categories = FormatProperties(GetProperty(attributes, "categories")),
                Magic = GetStringSafe(attributes, "magic"),
                TrID = ExtractTrID(attributes),
                ExifTool = ExtractExifTool(attributes)
            };

            var legacyHashes = GetProperty(attributes, "hashes");
            result.SHA256 = GetStringSafe(attributes, "sha256") ?? GetStringSafe(legacyHashes, "sha256") ?? hash;
            result.SHA1 = GetStringSafe(attributes, "sha1") ?? GetStringSafe(legacyHashes, "sha1");
            result.MD5 = GetStringSafe(attributes, "md5") ?? GetStringSafe(legacyHashes, "md5");

            result.TotalEngines = result.Malicious + result.Undetected + result.Suspicious + result.Harmless
                + result.Timeout + result.ConfirmedTimeout + result.Failure + result.TypeUnsupported;
            result.Summary = GenerateSummary(result);

            int totalVotes = result.CommunityVotesHarmless + result.CommunityVotesMalicious;
            result.CommunityVotesTotal = totalVotes;
            result.CommunityScore = totalVotes > 0 ? (double)result.CommunityVotesHarmless / totalVotes * 100 : 0;

            return result;
        }

        private static string GenerateSummary(VirusTotalScanResult result)
        {
            if (result.Malicious > 0 || result.Suspicious > 0)
                return $"Malicious: {result.Malicious} · Suspicious: {result.Suspicious} · Engines: {result.TotalEngines}";
            if (result.Undetected == 0 && result.Harmless == 0)
                return "VirusTotal has no completed engine verdicts for this file.";

            return $"No malicious or suspicious detections across {result.Undetected + result.Harmless} completed verdicts.";
        }

        /// <summary>Formats the UTC timestamps used by the VirusTotal file API.</summary>
        private static string FormatDate(JsonElement attributes, string name)
        {
            var value = GetProperty(attributes, name);
            return value.ValueKind == JsonValueKind.Number
                && value.TryGetInt64(out var seconds)
                && seconds >= DateTimeOffset.MinValue.ToUnixTimeSeconds()
                && seconds <= DateTimeOffset.MaxValue.ToUnixTimeSeconds()
                ? DateTimeOffset.FromUnixTimeSeconds(seconds).ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)
                : "N/A";
        }

        private static JsonElement GetProperty(JsonElement element, string name) =>
            element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) ? value : default;

        private static int? GetInteger(JsonElement element, string name)
        {
            var value = GetProperty(element, name);
            return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) ? number : null;
        }

        private static long GetLong(JsonElement element, string name)
        {
            var value = GetProperty(element, name);
            return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out long number) ? number : 0;
        }

        private static string? GetStringSafe(JsonElement element, string name)
        {
            var value = GetProperty(element, name);
            return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        }

        private static string? ExtractTags(JsonElement attributes)
        {
            var tags = GetProperty(attributes, "tags");
            return tags.ValueKind == JsonValueKind.Array
                ? string.Join(", ", tags.EnumerateArray().Where(tag => tag.ValueKind == JsonValueKind.String).Select(tag => tag.GetString()))
                : null;
        }

        private static List<(string Name, string? Category, string? Result)> ReadEngineResults(JsonElement attributes)
        {
            var results = new List<(string Name, string? Category, string? Result)>();
            var engines = GetProperty(attributes, "last_analysis_results");
            if (engines.ValueKind != JsonValueKind.Object)
                return results;

            foreach (var engine in engines.EnumerateObject())
            {
                if (engine.Value.ValueKind != JsonValueKind.Object)
                    continue;

                results.Add((GetStringSafe(engine.Value, "engine_name") ?? engine.Name,
                    GetStringSafe(engine.Value, "category"), GetStringSafe(engine.Value, "result")));
            }

            return results;
        }

        private static string? ExtractTrID(JsonElement attributes)
        {
            var types = GetProperty(attributes, "trid");
            if (types.ValueKind != JsonValueKind.Array)
                return null;

            var lines = new List<string>();
            foreach (var item in types.EnumerateArray())
            {
                var name = GetStringSafe(item, "file_type");
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var probability = GetProperty(item, "probability");
                lines.Add(probability.ValueKind == JsonValueKind.Number && probability.TryGetDouble(out var percent)
                    ? $"{name} ({percent:F1}%)"
                    : name);
            }

            return JoinLines(lines);
        }

        private static string? ExtractExifTool(JsonElement attributes) =>
            FormatProperties(GetProperty(attributes, "exiftool"));

        private static string? FormatProperties(JsonElement properties) =>
            properties.ValueKind == JsonValueKind.Object
                ? JoinLines(properties.EnumerateObject().Select(property => $"{property.Name}: {property.Value}"))
                : null;

        private static string? JoinLines(IEnumerable<string> lines)
        {
            var text = string.Join("\n", lines);
            return text.Length == 0 ? null : text;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // The shared HTTP client lives for the lifetime of the application.
            GC.SuppressFinalize(this);
        }
    }
}
