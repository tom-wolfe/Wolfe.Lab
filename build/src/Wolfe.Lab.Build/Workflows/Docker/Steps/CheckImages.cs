using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker.Steps;

/// <summary>
/// Fails an image whose context has no Dockerfile, or whose tag would push to Docker Hub.
/// </summary>
[Step("check images", StepKind.Check)]
internal sealed class CheckImages(ComponentImages component, IFileSystem fileSystem, IWorkflowLog log)
{
    internal const string Dockerfile = "Dockerfile";

    public StepResult Run()
    {
        List<string> problems = [];
        foreach (var image in component.Images)
        {
            if (!fileSystem.ProjectRoot.GetDirectory(image.Context).GetFile(Dockerfile).Exists)
            {
                problems.Add($"{image.Tag}: no {Dockerfile} in '{image.Context}'.");
            }

            if (component.Pushed && PushImages.Registry(image.Tag) is null)
            {
                problems.Add($"{image.Tag}: the tag names no registry host, so the push would go to Docker Hub.");
            }
        }

        if (problems.Count > 0)
        {
            return StepResult.Failed(string.Join(" ", problems));
        }

        log.Detail($"{component.Images.Count} image(s) ready to build{(component.Pushed ? " and push" : "")}.");
        return StepResult.Successful;
    }
}
