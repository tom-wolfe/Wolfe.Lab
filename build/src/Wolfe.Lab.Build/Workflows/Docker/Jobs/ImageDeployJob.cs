using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Ritten.Docker.Steps;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.Docker.Models;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Workflows.Docker.Jobs;

/// <summary>
/// Builds the component's images, and pushes them when the component names a registry.
/// </summary>
/// <remarks>
/// For a component that ships an image and no stack — the CI image, which every containerised
/// runner pulls from Forgejo's registry.
/// </remarks>
internal sealed class ImageDeployJob : LabJob<DockerSettings>
{
    public override string Name => "deploy";

    public override string Description => "Builds the component's images on this node, and pushes them to its registry.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<BuildImages>(), Step.FromType<GateApproval>(), Step.FromType<PushImages>()];

    public override JobKind Kind => JobKind.Deploy;

    protected override void ValidateSettings(SettingsValidator<DockerSettings> settings) =>
        ImageCheckJob.ValidateImages(settings);

    protected override void Configure(IWorkflowBuilder builder, DockerSettings settings)
    {
        base.Configure(builder, settings);
        DockerImage[] images = [.. settings.Images.Select(image => image.ToImage()).OfType<DockerImage>()];
        builder.AddDocker(images);
        builder.Services.AddSingleton(settings.Registry is { Username: { } username, Token: { } token }
            ? new RegistryPush(images, new RegistryCredential(username, token))
            : RegistryPush.None);
    }
}
