using Ritten.Docker;
using Ritten.Docker.Steps;
using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker.Jobs;

/// <summary>
/// Builds the component's images into the node's own image store.
/// </summary>
/// <remarks>
/// For a component that ships an image and no stack — the CI image, which the containerised
/// runner pulls by tag from the node it was built on.
/// </remarks>
internal sealed class BuildJob : LabJob<DockerSettings>
{
    public override string Name => "build";

    public override string Description => "Builds the component's images on this node.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<BuildImages>()];

    public override JobKind Kind => JobKind.Work;

    protected override void ValidateSettings(SettingsValidator<DockerSettings> settings) => settings
        .Require(s => s.Images.Count > 0, "an image component needs at least one entry in 'images'.")
        .Require(s => s.Images.All(image => image.ToImage() is not null), "every entry in 'images' needs a 'tag' and a 'context'.");

    protected override void Configure(IWorkflowBuilder builder, DockerSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker([.. settings.Images.Select(image => image.ToImage()).OfType<DockerImage>()]);
    }
}
