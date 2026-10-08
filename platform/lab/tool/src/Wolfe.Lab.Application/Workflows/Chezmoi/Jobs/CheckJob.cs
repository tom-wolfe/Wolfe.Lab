using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Application.Workflows.Chezmoi.Models;
using Wolfe.Lab.Application.Workflows.Chezmoi.Steps;
using Wolfe.Lab.Infrastructure.Chezmoi;
using Wolfe.Lab.Infrastructure.Packages;

namespace Wolfe.Lab.Application.Workflows.Chezmoi.Jobs;

/// <summary>
/// Proves the source renders for every machine and that what it renders is valid shell.
/// </summary>
internal sealed class CheckJob : LabJob<ChezmoiOptions>
{
    public override string Name => "check";

    public override string Description => "Renders the source for every profile, vault stubbed, and shellchecks the rendered scripts.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<GatePathFilter>(),
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<EnsureTools>(),
        Step.FromType<ResolveProfiles>(),
        Step.FromType<RenderProfiles>(),
        Step.FromType<CheckScripts>()
    ];

    public override JobKind Kind => JobKind.Check;

    public override bool RequiresProject => false;

    protected override void Configure(IWorkflowBuilder builder, ChezmoiOptions options)
    {
        base.Configure(builder, options);
        builder.AddChezmoi().AddTools("shellcheck");
        builder.Services.AddSingleton(options);
    }
}
