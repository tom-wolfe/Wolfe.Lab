using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Application.Workflows.Docker.Steps;

namespace Wolfe.Lab.Application.Workflows.Docker.Jobs;

/// <summary>
/// Proves an image component could be built and pushed.
/// </summary>
internal sealed class ImageCheckJob : LabJob<ImageComponentOptions>
{
    public override string Name => "check";

    public override string Description => "Checks the component's images each have a Dockerfile and a tag naming their registry.";

    public override IReadOnlyList<Step> Steps { get; } = [
        Step.FromType<GatePathFilter>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveImages>(),
        Step.FromType<CheckImages>()
    ];

    public override JobKind Kind => JobKind.Check;

    public override bool RequiresProject => false;

    protected override void Configure(IWorkflowBuilder builder, ImageComponentOptions options)
    {
        base.Configure(builder, options);
        builder.Services.AddSingleton(options);
    }
}
