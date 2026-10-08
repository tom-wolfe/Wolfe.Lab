using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Images;

namespace Wolfe.Lab.Application.Workflows.Docker.Steps;

/// <summary>
/// Checks the image the component declares has its Dockerfile; its tag names a registry already,
/// or it would not have been read.
/// </summary>
[Step("check images", StepKind.Check)]
internal sealed class CheckImages(IFileSystem fileSystem, IWorkflowLog log)
{
    public StepResult Run(DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(WorkflowName.Image).TryGetValue(out var declared, out var errors))
        {
            return StepResult.Failed(errors);
        }

        var image = (ImageComponent)declared;
        if (!fileSystem.ProjectRoot.GetDirectory(image.Context).GetFile(image.Dockerfile).Exists)
        {
            return new Error($"{image.Tag}: no {image.Dockerfile} in '{image.Context}'.");
        }

        log.Detail($"{image.Tag} is ready to build and push, to {image.Tag.Registry}.");
        return StepResult.Successful;
    }
}
