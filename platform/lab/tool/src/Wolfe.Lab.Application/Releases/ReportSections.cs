namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// The sections of a run's report that more than one step writes to.
/// </summary>
internal static class ReportSections
{
    /// <summary>
    /// What a release published, and what it restarted for it.
    /// </summary>
    public const string Artifacts = "Artifacts";
}
