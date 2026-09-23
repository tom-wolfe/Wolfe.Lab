namespace Wolfe.Lab.Build.Workflows.ImmichImport.Models;

/// <summary>
/// The shape of the import component's <c>ritten.json</c>: <c>"workflow": "immich-import"</c>.
/// </summary>
public sealed record ImmichImportSettings : WorkflowSettings
{
    /// <summary>
    /// The server the Takeout goes into.
    /// </summary>
    public ServerSettings Server { get; init; } = new();

    /// <summary>
    /// Where the Takeout's zip parts are.
    /// </summary>
    public TakeoutSettings Takeout { get; init; } = new();

    /// <summary>
    /// How the import runs.
    /// </summary>
    public ImportSettings Import { get; init; } = new();
}
