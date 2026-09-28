using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Build.Clients.Gates.Steps;
using Wolfe.Lab.Build.Workflows.Docker.Models;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Workflows.Docker.Jobs;

/// <summary>
/// Proves an image component could be built and pushed.
/// </summary>
internal sealed class ImageCheckJob : LabJob<DockerSettings>
{
    public override string Name => "check";

    public override string Description => "Checks the component's images each have a Dockerfile and a tag naming their registry.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<GatePathFilter>(), Step.FromType<CheckImages>()];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<DockerSettings> settings) => settings
        .Require(s => s.Images.Count > 0, "an image component needs at least one entry in 'images'.")
        .Require(s => s.Images.All(image => image.ToImage() is not null), "every entry in 'images' needs a 'tag' and a 'context'.");

    protected override void Configure(IWorkflowBuilder builder, DockerSettings settings)
    {
        base.Configure(builder, settings);
        builder.Services.AddSingleton(new ComponentImages([.. settings.Images.Select(image =>
            new ComponentImage(image.Tag!, image.Context!, image.Dockerfile ?? CheckImages.Dockerfile))]));
    }
}
