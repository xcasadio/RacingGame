namespace RacingGameCasaEngine.Bootstrap;

internal sealed class RaceLaunchOptions
{
    public bool ValidateFrontEndNavigation { get; init; }

    public bool CaptureTrackAudit { get; init; }

    public bool CaptureUiScreens { get; init; }

    public bool CaptureCarProfileAudit { get; init; }

    public bool CaptureCarTopSpeedAudit { get; init; }

    public bool ExportTrackRuntimeScene { get; init; }

    public string? RuntimeSceneExportFilePath { get; init; }

    public string? CarProfileAuditFilePath { get; init; }

    public string? CarTopSpeedAuditFilePath { get; init; }

    public string? CarTopSpeedAuditTrackName { get; init; }

    /// <summary>--capture-reference-views: replay the views exported by the original game (ADR-0015).</summary>
    public bool CaptureReferenceViews { get; init; }

    public string? ReferenceViewsFilePath { get; init; }

    public string? ReferenceCaptureOutputDirectory { get; init; }
}