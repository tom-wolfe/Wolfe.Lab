using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Deploy;
using Wolfe.Lab.Build.Deploy.Steps;
using Wolfe.Lab.Build.Slices;
using Wolfe.Lab.Build.Slices.Steps;
using Wolfe.Lab.Build.Workflows.Common.Steps;
using Wolfe.Lab.Build.Workflows.Docker.Models;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Workflows.Docker.Jobs;

/// <summary>
/// Installs a compose component on the node and converges its stack.
/// </summary>
internal sealed class DeployJob<TSettings> : LabJob<TSettings> where TSettings : DockerSettings
{
    public override string Name => "deploy";

    public override string Description => "Builds the component's images, installs it on the node and converges its stack.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveStack>(),
        Step.FromType<CheckVolumes>(),
        Step.FromType<InstallSlice>(),
        Step.FromType<BuildImages>(),
        Step.FromType<ResolveComposeSecrets>(),
        Step.FromType<GateApproval>(),
        Step.FromType<ComposeUp>()
    ];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<TSettings> settings) => settings
        .Require(s => s.Images.All(image => image.ToImage() is not null), "every entry in 'images' needs a 'tag' and a 'context'.");

    protected override void Configure(IWorkflowBuilder builder, TSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker().AddSliceInstaller();
        builder.Services.AddSingleton<DockerSettings>(settings);
        builder.Services.AddSingleton(new RequiredVolumes([.. settings.Volumes.Select(volume => volume.Directory)]));
        builder.Services.AddSingleton(new ImagePlan([.. settings.Images.Select(image => image.ToImage()).OfType<BuildableImage>()]));
    }
}
