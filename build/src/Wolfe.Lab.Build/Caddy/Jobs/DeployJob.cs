using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Caddy.Models;
using Wolfe.Lab.Build.Caddy.Steps;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Deploy.Services;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Steps;

namespace Wolfe.Lab.Build.Caddy.Jobs;

/// <summary>
/// Deploys the front door: an ordinary slice deploy, plus the two things only the door does —
/// collecting every slice's route and telling the running container to read them.
/// </summary>
internal sealed class DeployJob : LabJob<CaddySettings>
{
    public override string Name => "deploy";

    public override string Description => "Installs the front door, converges it, gathers every slice's route and reloads.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveSlice>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<InstallSlice>(),
        Step.FromType<ResolveComposeSecrets>(),
        Step.FromType<GateApproval>(),
        Step.FromType<ComposeUp>(),
        Step.FromType<GatherRoutes>(),
        Step.FromType<ReloadCaddy>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void Configure(IWorkflowBuilder builder, CaddySettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker().AddSliceInstaller();
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(volume => volume.Directory)]));
    }
}
