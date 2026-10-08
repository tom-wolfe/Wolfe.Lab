using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.Docker.Models;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Images;

namespace Wolfe.Lab.Application.Workflows.Docker.Steps;

/// <summary>
/// The image the component declares, or those its <c>ritten.json</c> lists while it declares none.
/// </summary>
[Step("resolve images", StepKind.Work)]
internal sealed class ResolveImages(DeclaredComponents declared, ImageComponentOptions legacy)
{
    public async Task<StepResult<ComponentImages>> Run(ServiceCatalog catalog, CancellationToken ct = default)
    {
        if (await declared.Find<ImageComponent>(catalog, ct) is { } image)
        {
            return new ComponentImages([new ComponentImage(image.Tag.Value, image.Context, image.Dockerfile)]);
        }

        var images = legacy.Images
            .Where(written => written is { Tag.Length: > 0, Context.Length: > 0 })
            .Select(written => new ComponentImage(written.Tag!, written.Context!, written.Dockerfile ?? CheckImages.Dockerfile))
            .ToList();
        return images.Count > 0 ? new ComponentImages(images) : new Error("The component declares no image.");
    }
}
