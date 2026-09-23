using Ritten.Docker;
using Ritten.Docker.Steps;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Clients.Releases;
using Wolfe.Lab.Build.Clients.Releases.Steps;
using Wolfe.Lab.Build.Clients.Volumes;
using Wolfe.Lab.Build.Clients.Volumes.Steps;
using Wolfe.Lab.Build.Workflows.Docker.Models;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Workflows.Docker.Jobs;

/// <summary>
/// Installs a compose component on the node and converges its stack.
/// </summary>
/// <remarks>
/// The component is installed rather than run from the checkout because containers bind-mount
/// files out of it and go on reading them after the job that deployed them is gone. Images are
/// built from the checkout first, because a build context is source and the installer does not
/// carry source onto the node.
/// </remarks>
internal sealed class DeployJob<TSettings> : LabJob<TSettings> where TSettings : DockerSettings
{
    public override string Name => "deploy";

    public override string Description => "Builds the component's images, installs it on the node and converges its stack.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveRelease>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<InstallRelease>(),
        Step.FromType<BuildImages>(),
        Step.FromType<ResolveComposeSecrets>(),
        Step.FromType<GateApproval>(),
        Step.FromType<ConvergeRelease>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<TSettings> settings) => settings
        .Require(s => s.Release is { Length: > 0 }, "'release' not set in ritten.json: the name the component is installed under.")
        .Require(s => s.Images.All(image => image.ToImage() is not null), "every entry in 'images' needs a 'tag' and a 'context'.");

    protected override void Configure(IWorkflowBuilder builder, TSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker([.. settings.Images.Select(image => image.ToImage()).OfType<DockerImage>()]).AddVolumes(settings.Volumes);
        if (settings.Release is { Length: > 0 } release)
        {
            builder.AddReleases(release);
        }
    }
}
