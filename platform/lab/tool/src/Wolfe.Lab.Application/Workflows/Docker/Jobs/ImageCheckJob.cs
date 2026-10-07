using Microsoft.Extensions.DependencyInjection;
using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Gates;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Application.Workflows.Docker.Steps;

namespace Wolfe.Lab.Application.Workflows.Docker.Jobs;

/// <summary>
/// Proves an image component could be built and pushed.
/// </summary>
internal sealed class ImageCheckJob : LabJob<ImageComponentOptions>
{
    public override string Name => "check";

    public override string Description => "Checks the component's images each have a Dockerfile and a tag naming their registry.";

    public override IReadOnlyList<Step> Steps { get; } = [Step.FromType<GatePathFilter>(), Step.FromType<ResolveServiceCatalog>(), Step.FromType<CheckImages>()];

    public override JobKind Kind => JobKind.Check;

    protected override void ValidateSettings(SettingsValidator<ImageComponentOptions> options) => options
        .Require(s => s.Images.Count > 0, "an image component needs at least one entry in 'images'.")
        .Require(s => s.Images.All(image => image is { Tag.Length: > 0, Context.Length: > 0 }), "every entry in 'images' needs a 'tag' and a 'context'.");

    protected override void Configure(IWorkflowBuilder builder, ImageComponentOptions options)
    {
        base.Configure(builder, options);
        builder.Services.AddSingleton(new ComponentImages([.. options.Images.SelectMany(IEnumerable<ComponentImage> (image) =>
            image is { Tag: { Length: > 0 } tag, Context: { Length: > 0 } context }
                ? [new ComponentImage(tag, context, image.Dockerfile ?? CheckImages.Dockerfile)]
                : [])]));
    }
}
