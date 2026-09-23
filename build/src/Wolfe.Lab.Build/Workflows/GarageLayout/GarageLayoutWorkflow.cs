using Wolfe.Lab.Build.Workflows.GarageLayout.Jobs;

namespace Wolfe.Lab.Build.Workflows.GarageLayout;

/// <summary>
/// Garage's cluster layout: <c>"workflow": "garage-layout"</c>.
/// </summary>
public sealed class GarageLayoutWorkflow : IWorkflow
{
    /// <inheritdoc />
    public string Name => "garage-layout";

    /// <inheritdoc />
    public string Label => "garage layout";

    /// <inheritdoc />
    public IReadOnlyList<IJob> Jobs { get; } = [new InitJob()];
}
