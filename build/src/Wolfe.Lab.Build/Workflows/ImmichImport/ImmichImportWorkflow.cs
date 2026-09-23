using Wolfe.Lab.Build.Workflows.ImmichImport.Jobs;

namespace Wolfe.Lab.Build.Workflows.ImmichImport;

/// <summary>
/// The Google Takeout, into Immich: <c>"workflow": "immich-import"</c>.
/// </summary>
public sealed class ImmichImportWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "immich-import";

    /// <inheritdoc />
    public string Label => "immich import";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new ImportJob()];
}
