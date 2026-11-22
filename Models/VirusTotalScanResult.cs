using System.Text.Json.Serialization;

namespace WTHIT.Models
{
    /// <summary>
    /// Represents a VirusTotal file report or the reason a lookup failed.
    /// </summary>
    public class VirusTotalScanResult
    {
        public string Summary { get; set; } = string.Empty;
        public bool? ReportAvailable { get; set; }

        /// <summary>Recognizes completed reports saved before the lookup status was persisted.</summary>
        [JsonIgnore]
        public bool HasReport => ReportAvailable ??
            (TotalEngines > 0 || Malicious > 0 || Suspicious > 0 || Harmless > 0 || Undetected > 0);

        public int Malicious { get; set; }
        public int Undetected { get; set; }
        public int Suspicious { get; set; }
        public int Harmless { get; set; }
        public int Timeout { get; set; }
        public int ConfirmedTimeout { get; set; }
        public int Failure { get; set; }
        public int TypeUnsupported { get; set; }
        public string? EngineResults { get; set; }
        public int TotalEngines { get; set; }
        public string? MeaningfulName { get; set; }
        public string? TypeDescription { get; set; }
        public string? Tags { get; set; }
        public string? SHA256 { get; set; }
        public string? SHA1 { get; set; }
        public string? MD5 { get; set; }
        public long Size { get; set; }
        public string FirstSubmissionDate { get; set; } = string.Empty;
        public string LastSubmissionDate { get; set; } = string.Empty;
        public string LastAnalysisDate { get; set; } = string.Empty;
        public int TimesSubmitted { get; set; }
        public double CommunityScore { get; set; }
        public int CommunityVotesTotal { get; set; }
        public int CommunityVotesHarmless { get; set; }
        public int CommunityVotesMalicious { get; set; }
        public int Reputation { get; set; }
        public string? ThreatNames { get; set; }
        public string? Categories { get; set; }
        public string? Magic { get; set; }
        public string? TrID { get; set; }
        public string? ExifTool { get; set; }
    }
}
