namespace Wolfe.Lab.Application.Releases;

/// <summary>
/// The sections of a run's report that more than one step writes to.
/// </summary>
internal static class ReportSections
{
    /// <summary>
    /// What a deploy installed on the node, and what it restarted for it.
    /// </summary>
    public const string Install = "Install";
}
