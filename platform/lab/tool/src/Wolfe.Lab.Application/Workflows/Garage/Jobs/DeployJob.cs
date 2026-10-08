using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.Garage.Steps;
using Wolfe.Lab.Infrastructure.Garage;

namespace Wolfe.Lab.Application.Workflows.Garage.Jobs;

/// <summary>
/// Deploys Garage as the docker workflow does, then brings the node's layout into line.
/// </summary>
internal sealed class DeployJob : Docker.Jobs.DeployJob<DeclaredSettings>
{
    public override string Description => "Builds and converges Garage's stack, then brings its node's role in the cluster layout into line.";

    public override IReadOnlyList<Step> Steps => field ??= [.. base.Steps, Step.FromType<AwaitGarage>(), Step.FromType<ConvergeLayout>()];

    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddGarage();
    }
}
