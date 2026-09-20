using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Deploy.Services;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Steps;

namespace Wolfe.Lab.Build.Deploy.Jobs;

/// <summary>
/// Installs the slice on the node and converges its compose stack: the whole of what
/// <c>scripts/deploy.sh</c> did, for any slice whose <c>ritten.json</c> names its volumes.
/// </summary>
/// <remarks>
/// The slice is installed rather than run from the checkout because containers bind-mount
/// files out of it — config directories, route snippets — and go on reading them long after
/// the job that deployed them is gone.
/// </remarks>
/// <typeparam name="TSettings">The slice's <c>ritten.json</c> shape.</typeparam>
internal sealed class DeployJob<TSettings> : LabJob<TSettings> where TSettings : SliceSettings
{
    public override string Name => "deploy";

    public override string Description => "Installs the slice on the node and converges its compose stack.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveSlice>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<InstallSlice>(),
        Step.FromType<ResolveComposeSecrets>(),
        Step.FromType<ApprovalGate>(),
        Step.FromType<ComposeUp>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void Configure(IWorkflowBuilder builder, TSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker().AddSliceInstaller();
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(volume => volume.Directory)]));
    }
}
