using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WTHIT.Models;

namespace WTHIT.Controls;

public sealed partial class ScanReportView : UserControl, INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public ScanReportView()
    {
        this.InitializeComponent();
    }

#region Process Icon
    private UIElement? _processIconElement;
    public UIElement? ProcessIconElement 
    { 
        get => _processIconElement; 
        set => SetProperty(ref _processIconElement, value); 
    }
    #endregion

    #region Header

    private string _processName = string.Empty;
    public string ProcessName { get => _processName; set => SetProperty(ref _processName, value); }

    private string _reportSummary = string.Empty;
    public string ReportSummary { get => _reportSummary; set => SetProperty(ref _reportSummary, value); }

    private Uri? _reportUri;
    public Uri? ReportUri
    {
        get => _reportUri;
        set { if (SetProperty(ref _reportUri, value)) OnPropertyChanged(nameof(HasReportLink)); }
    }

    public bool HasReportLink => ReportUri != null;

    private bool _hasReport;
    public bool HasReport { get => _hasReport; set => SetProperty(ref _hasReport, value); }

    #endregion

    #region Detection stats

    private int _maliciousCount;
    public int MaliciousCount { get => _maliciousCount; set { if (SetProperty(ref _maliciousCount, value)) RaiseDetectionDependents(); } }

    private int _suspiciousCount;
    public int SuspiciousCount { get => _suspiciousCount; set { if (SetProperty(ref _suspiciousCount, value)) RaiseDetectionDependents(); } }

    private int _harmlessCount;
    public int HarmlessCount { get => _harmlessCount; set { if (SetProperty(ref _harmlessCount, value)) RaiseDetectionDependents(); } }

    private int _undetectedCount;
    public int UndetectedCount { get => _undetectedCount; set { if (SetProperty(ref _undetectedCount, value)) RaiseDetectionDependents(); } }

    private int _totalEngines;
    public int TotalEngines { get => _totalEngines; set { if (SetProperty(ref _totalEngines, value)) RaiseDetectionDependents(); } }

    private string _engineResults = string.Empty;
    public string EngineResults
    {
        get => _engineResults;
        set { if (SetProperty(ref _engineResults, value)) OnPropertyChanged(nameof(HasEngineResults)); }
    }

    public bool HasEngineResults => EngineResults.Length > 0;

    private string _incompleteResults = string.Empty;
    public string IncompleteResults
    {
        get => _incompleteResults;
        set { if (SetProperty(ref _incompleteResults, value)) OnPropertyChanged(nameof(HasIncompleteResults)); }
    }

    public bool HasIncompleteResults => IncompleteResults.Length > 0;

    public bool HasDetectionData => MaliciousCount + SuspiciousCount + HarmlessCount + UndetectedCount > 0;

    public string EngineCountText => $"{MaliciousCount + SuspiciousCount} of {TotalEngines} security vendors flagged this file";

    public GridLength MaliciousWidth => new(TotalEngines > 0 ? MaliciousCount : 0, GridUnitType.Star);
    public GridLength SuspiciousWidth => new(TotalEngines > 0 ? SuspiciousCount : 0, GridUnitType.Star);
    public GridLength HarmlessWidth => new(TotalEngines > 0 ? HarmlessCount : 0, GridUnitType.Star);
    public GridLength UndetectedWidth => new(TotalEngines > 0 ? UndetectedCount : 1, GridUnitType.Star);

    private void RaiseDetectionDependents()
    {
        OnPropertyChanged(nameof(HasDetectionData));
        OnPropertyChanged(nameof(EngineCountText));
        OnPropertyChanged(nameof(MaliciousWidth));
        OnPropertyChanged(nameof(SuspiciousWidth));
        OnPropertyChanged(nameof(HarmlessWidth));
        OnPropertyChanged(nameof(UndetectedWidth));
    }

    #endregion

    #region Threats

    private bool _hasThreats;
    public bool HasThreats { get => _hasThreats; set => SetProperty(ref _hasThreats, value); }

    private string _threatNames = string.Empty;
    public string ThreatNames { get => _threatNames; set => SetProperty(ref _threatNames, value); }

    #endregion

    #region File information

    private string _meaningfulName = "N/A";
    public string MeaningfulName { get => _meaningfulName; set => SetProperty(ref _meaningfulName, value); }

    private string _typeDescription = "N/A";
    public string TypeDescription { get => _typeDescription; set => SetProperty(ref _typeDescription, value); }

    private string _categories = "N/A";
    public string Categories { get => _categories; set => SetProperty(ref _categories, value); }

    private string _tags = "N/A";
    public string Tags { get => _tags; set => SetProperty(ref _tags, value); }

    private string _sizeDisplay = "N/A";
    public string SizeDisplay { get => _sizeDisplay; set => SetProperty(ref _sizeDisplay, value); }

    #endregion

    #region Submission history

    private string _firstSubmissionDate = "N/A";
    public string FirstSubmissionDate { get => _firstSubmissionDate; set => SetProperty(ref _firstSubmissionDate, value); }

    private string _lastSubmissionDate = "N/A";
    public string LastSubmissionDate { get => _lastSubmissionDate; set => SetProperty(ref _lastSubmissionDate, value); }

    private string _lastAnalysisDate = "N/A";
    public string LastAnalysisDate { get => _lastAnalysisDate; set => SetProperty(ref _lastAnalysisDate, value); }

    private int _timesSubmitted;
    public int TimesSubmitted { get => _timesSubmitted; set => SetProperty(ref _timesSubmitted, value); }

    #endregion

    #region Community reputation

    private double _communityScore;
    public double CommunityScore { get => _communityScore; set { if (SetProperty(ref _communityScore, value)) OnPropertyChanged(nameof(CommunityScoreText)); } }
    public string CommunityScoreText => CommunityVotesTotal > 0 ? $"{CommunityScore:0.0}%" : "N/A";

    private int _communityVotesTotal;
    public int CommunityVotesTotal
    {
        get => _communityVotesTotal;
        set { if (SetProperty(ref _communityVotesTotal, value)) OnPropertyChanged(nameof(CommunityScoreText)); }
    }

    private int _communityVotesHarmless;
    public int CommunityVotesHarmless { get => _communityVotesHarmless; set { if (SetProperty(ref _communityVotesHarmless, value)) OnPropertyChanged(nameof(VotesBreakdownText)); } }

    private int _communityVotesMalicious;
    public int CommunityVotesMalicious { get => _communityVotesMalicious; set { if (SetProperty(ref _communityVotesMalicious, value)) OnPropertyChanged(nameof(VotesBreakdownText)); } }

    public string VotesBreakdownText => $"{CommunityVotesHarmless} / {CommunityVotesMalicious}";

    private int _reputation;
    public int Reputation { get => _reputation; set => SetProperty(ref _reputation, value); }

    private bool _hasCommunityData;
    public bool HasCommunityData { get => _hasCommunityData; set => SetProperty(ref _hasCommunityData, value); }

    #endregion

    #region Additional details

    private string _magic = "N/A";
    public string Magic { get => _magic; set => SetProperty(ref _magic, value); }

    private string _trId = "N/A";
    public string TrID { get => _trId; set => SetProperty(ref _trId, value); }

    private string _exifTool = "N/A";
    public string ExifTool { get => _exifTool; set => SetProperty(ref _exifTool, value); }

    private bool _hasAdditionalDetails;
    public bool HasAdditionalDetails { get => _hasAdditionalDetails; set => SetProperty(ref _hasAdditionalDetails, value); }

    #endregion

    #region Hashes

    private string _md5 = "N/A";
    public string MD5 { get => _md5; set => SetProperty(ref _md5, value); }

    private string _sha1 = "N/A";
    public string SHA1 { get => _sha1; set => SetProperty(ref _sha1, value); }

    private string _sha256 = "N/A";
    public string SHA256 { get => _sha256; set => SetProperty(ref _sha256, value); }

    #endregion

    #region Population

    /// <summary>Displays the metadata and engine outcomes available in the file report.</summary>
    public void PopulateFromScanResult(ScanResultItem item)
    {
        if (item == null) return;

        ProcessName = item.ProcessName;

        var d = item.ScanDetails;
        if (d is not { HasReport: true })
        {
            ResetDetails();
            HasReport = false;
            ReportSummary = d?.Summary ?? item.ScanResult;
            return;
        }

        HasReport = true;
        ReportSummary = d.ReportAvailable == true
            ? d.Summary
            : "Saved VirusTotal report";
        ReportUri = d.SHA256 is { Length: 64 } hash && hash.All(Uri.IsHexDigit)
            ? new Uri($"https://www.virustotal.com/gui/file/{hash}")
            : null;

        MaliciousCount = d.Malicious;
        SuspiciousCount = d.Suspicious;
        HarmlessCount = d.Harmless;
        UndetectedCount = d.Undetected;
        TotalEngines = d.TotalEngines > 0
            ? d.TotalEngines
            : d.Malicious + d.Suspicious + d.Harmless + d.Undetected;
        EngineResults = d.EngineResults ?? string.Empty;
        IncompleteResults = d.Timeout + d.ConfirmedTimeout + d.Failure + d.TypeUnsupported > 0
            ? $"Timeout: {d.Timeout} · Confirmed timeout: {d.ConfirmedTimeout} · Failed: {d.Failure} · Unsupported: {d.TypeUnsupported}"
            : string.Empty;

        HasThreats = d.Malicious > 0 || d.Suspicious > 0;
        ThreatNames = string.IsNullOrWhiteSpace(d.ThreatNames) ? "No specific threat names reported." : d.ThreatNames;

        MeaningfulName = string.IsNullOrWhiteSpace(d.MeaningfulName) ? "N/A" : d.MeaningfulName;
        TypeDescription = string.IsNullOrWhiteSpace(d.TypeDescription) ? "N/A" : d.TypeDescription;
        Categories = string.IsNullOrWhiteSpace(d.Categories) ? "N/A" : d.Categories;
        Tags = string.IsNullOrWhiteSpace(d.Tags) ? "N/A" : d.Tags;
        SizeDisplay = FormatBytes(d.Size);

        FirstSubmissionDate = string.IsNullOrWhiteSpace(d.FirstSubmissionDate) ? "N/A" : d.FirstSubmissionDate;
        LastSubmissionDate = string.IsNullOrWhiteSpace(d.LastSubmissionDate) ? "N/A" : d.LastSubmissionDate;
        LastAnalysisDate = string.IsNullOrWhiteSpace(d.LastAnalysisDate) ? "N/A" : d.LastAnalysisDate;
        TimesSubmitted = d.TimesSubmitted;

        CommunityScore = d.CommunityScore;
        CommunityVotesTotal = d.CommunityVotesTotal;
        CommunityVotesHarmless = d.CommunityVotesHarmless;
        CommunityVotesMalicious = d.CommunityVotesMalicious;
        Reputation = d.Reputation;
        HasCommunityData = d.CommunityVotesTotal > 0 || d.Reputation != 0 || d.CommunityScore != 0;

        Magic = string.IsNullOrWhiteSpace(d.Magic) ? "N/A" : d.Magic;
        TrID = string.IsNullOrWhiteSpace(d.TrID) ? "N/A" : d.TrID;
        ExifTool = string.IsNullOrWhiteSpace(d.ExifTool) ? "N/A" : d.ExifTool;
        HasAdditionalDetails = !string.IsNullOrWhiteSpace(d.Magic)
            || !string.IsNullOrWhiteSpace(d.TrID)
            || !string.IsNullOrWhiteSpace(d.ExifTool);

        MD5 = string.IsNullOrWhiteSpace(d.MD5) ? "N/A" : d.MD5;
        SHA1 = string.IsNullOrWhiteSpace(d.SHA1) ? "N/A" : d.SHA1;
        SHA256 = string.IsNullOrWhiteSpace(d.SHA256) ? "N/A" : d.SHA256;
    }

    private void ResetDetails()
    {
        ReportSummary = EngineResults = IncompleteResults = string.Empty;
        ReportUri = null;
        MaliciousCount = SuspiciousCount = HarmlessCount = UndetectedCount = TotalEngines = 0;
        HasThreats = false;
        ThreatNames = string.Empty;
        MeaningfulName = TypeDescription = Categories = Tags = "N/A";
        SizeDisplay = "N/A";
        FirstSubmissionDate = LastSubmissionDate = LastAnalysisDate = "N/A";
        TimesSubmitted = 0;
        CommunityScore = 0;
        CommunityVotesTotal = CommunityVotesHarmless = CommunityVotesMalicious = 0;
        Reputation = 0;
        HasCommunityData = false;
        Magic = TrID = ExifTool = "N/A";
        HasAdditionalDetails = false;
        MD5 = SHA1 = SHA256 = "N/A";
    }

    #endregion

    #region Helpers

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "N/A";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }
        return $"{size:0.##} {units[unitIndex]} ({bytes:N0} bytes)";
    }

    #endregion
}
