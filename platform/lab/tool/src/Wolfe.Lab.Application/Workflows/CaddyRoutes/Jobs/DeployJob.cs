using Ritten.Docker;
using Wolfe.Lab.Application.Caddy;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Releases;
using Wolfe.Lab.Application.Workflows.CaddyRoutes.Steps;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.CaddyRoutes.Jobs;

/// <summary>
/// Republishes every component's route and tells the running caddy to read them.
/// </summary>
/// <remarks>
/// Snippets arrive through a bind mount and never change compose's config hash, so the door
/// has to be told; a plain redeploy of the Docker component would notice nothing.
/// </remarks>
internal sealed class DeployJob : LabJob<DeclaredSettings>
{
    public override string Name => "deploy";

    public override string Description => "Gathers every component's route snippet, installs them beside the front door and reloads it.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveServiceCatalog>(),
        Step.FromType<ResolveDeploymentUnit>(),
        Step.FromType<ResolveCaddy>(),
        Step.FromType<GatherRoutes>(),
        Step.FromType<GateApproval>(),
        Step.FromType<InstallRoutes>(),
        Step.FromType<ReloadCaddy>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void Configure(IWorkflowBuilder builder, DeclaredSettings options)
    {
        base.Configure(builder, options);
        builder.AddDocker().AddInstaller();
    }
}
