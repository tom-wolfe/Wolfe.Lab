using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Wolfe.Lab.Build.Workflows.Docker.Models;
using Wolfe.Lab.Build.Workflows.Docker.Steps;

namespace Wolfe.Lab.Build.Workflows.Docker.Jobs;

/// <summary>
/// Builds the component's images into the node's own image store.
/// </summary>
/// <remarks>
/// Work rather than a deploy, and named for what it does: nothing here publishes. Building an
/// image changes only the node's own image store, which is why the docker client's rehearsal
/// builds for real instead of skipping — and it is also the delivery, since the only node that
/// reads this image is the node that built it. No registry for the same reason the watcher's
/// image needs none.
/// </remarks>
internal sealed class BuildJob : LabJob<DockerSettings>
{
    public override string Name => "build";

    public override string Description => "Builds the component's images on this node.";

    public override IReadOnlyList<Step> Steps { get; } =
    [
        Step.FromType<ResolveStack>(),
        Step.FromType<BuildImages>()
    ];

    public override JobKind Kind => JobKind.Work;

    protected override void ValidateSettings(SettingsValidator<DockerSettings> settings) => settings
        .Require(s => s.Images.Count > 0, "an image component needs at least one entry in 'images'.")
        .Require(s => s.Images.All(image => image.ToImage() is not null), "every entry in 'images' needs a 'tag' and a 'context'.");

    protected override void Configure(IWorkflowBuilder builder, DockerSettings settings)
    {
        base.Configure(builder, settings);
        builder.AddDocker();
        builder.Services.AddSingleton(settings);
        builder.Services.AddSingleton(new ImagePlan([.. settings.Images.Select(image => image.ToImage()).OfType<BuildableImage>()]));
    }
}
