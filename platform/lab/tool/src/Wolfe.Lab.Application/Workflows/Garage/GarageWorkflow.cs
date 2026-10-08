using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.Docker.Jobs;

namespace Wolfe.Lab.Application.Workflows.Garage;

/// <summary>
/// Garage: <c>"workflow": "garage"</c>.
/// </summary>
/// <remarks>
/// The docker workflow, and then the cluster's layout: a new node is not done when the container
/// is up, but when Garage knows it stores something.
/// </remarks>
public sealed class GarageWorkflow : LabWorkflow
{
    /// <inheritdoc />
    public override string Name => "garage";

    /// <inheritdoc />
    public override string Label => "garage";

    /// <inheritdoc />
    public override IReadOnlyList<IJob> Jobs { get; } = [new CheckJob<DeclaredSettings>(), new Jobs.DeployJob()];
}
