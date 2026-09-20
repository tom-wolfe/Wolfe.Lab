using Ritten.Docker;
using Wolfe.Lab.Build.Deploy.Models;
using Wolfe.Lab.Build.Docker.Models;

namespace Wolfe.Lab.Build.Docker.Steps;

/// <summary>
/// Builds the images this component declares, from the checkout.
/// </summary>
/// <remarks>
/// From the CHECKOUT rather than the release, because a build context is source and the
/// installer does not carry source onto the node. A component that declares no images — most
/// of them — does nothing here.
/// </remarks>
[Step("build images", StepKind.Work)]
internal sealed class BuildImages(ImagePlan plan, IDocker docker, IWorkflowLog log)
{
    public async Task<StepResult> Run(Slice slice, CancellationToken ct = default)
    {
        if (plan.Images.Count == 0)
        {
            log.Detail("No images to build.");
            return StepResult.Successful;
        }

        foreach (var image in plan.Images)
        {
            var context = slice.Source.GetDirectory(image.Context);
            if (!context.GetFile("Dockerfile").Exists)
            {
                return new Error($"{context.AbsolutePath} has no Dockerfile.");
            }

            await docker.Build(context, image.Tag, ct: ct);
            log.Status($"Built {image.Tag}.");
        }

        return StepResult.Successful;
    }
}
