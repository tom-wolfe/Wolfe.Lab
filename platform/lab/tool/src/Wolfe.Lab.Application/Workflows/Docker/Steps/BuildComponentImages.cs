using Ritten.Docker;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Compose;

namespace Wolfe.Lab.Application.Workflows.Docker.Steps;

/// <summary>
/// Builds the image of each component the lab builds from source, out of the deployment's
/// directory in the checkout, under the name the catalog gives it.
/// </summary>
/// <remarks>
/// Built before the stack converges, always: compose only builds an image it cannot find, so a
/// source change left to compose would deploy the previous image and say nothing. The build
/// context is source, which the install does not carry onto the node in a usable form, so it is
/// the checkout's.
/// </remarks>
[Step("build images", StepKind.Work)]
internal sealed class BuildComponentImages(IDocker docker, IFileSystem fileSystem, IWorkflowLog log)
{
    internal const string Dockerfile = "Dockerfile";

    public async Task<StepResult> Run(DeploymentUnit unit, CancellationToken ct = default)
    {
        var images = unit.Components.OfType<DotNetServiceComponent>().Select(component => component.Image).ToList();
        if (images.Count == 0)
        {
            log.Detail("No images to build.");
            return StepResult.Successful;
        }

        var context = fileSystem.ProjectRoot;
        if (!context.GetFile(Dockerfile).Exists)
        {
            return new Error($"{unit} builds {string.Join(", ", images)}, but has no {Dockerfile}.");
        }

        foreach (var image in images)
        {
            await docker.Build(context, image, ct: ct);
            log.Status($"Built {image}.");
        }

        return StepResult.Successful;
    }
}
