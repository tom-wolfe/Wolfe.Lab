using Wolfe.Lab.Application.Workflows.Docker.Models;

namespace Wolfe.Lab.Application.Workflows.Docker.Steps;

/// <summary>
/// Fails an image whose Dockerfile is missing, or whose tag would push to Docker Hub.
/// </summary>
[Step("check images", StepKind.Check)]
internal sealed class CheckImages(IFileSystem fileSystem, IWorkflowLog log)
{
    internal const string Dockerfile = "Dockerfile";

    public StepResult Run(ComponentImages component)
    {
        List<string> problems = [];
        foreach (var image in component.Images)
        {
            if (!fileSystem.ProjectRoot.GetDirectory(image.Context).GetFile(image.Dockerfile).Exists)
            {
                problems.Add($"{image.Tag}: no {image.Dockerfile} in '{image.Context}'.");
            }

            if (Registry(image.Tag) is null)
            {
                problems.Add($"{image.Tag}: the tag names no registry host, so the push would go to Docker Hub.");
            }
        }

        if (problems.Count > 0)
        {
            return StepResult.Failed(string.Join(" ", problems));
        }

        log.Detail($"{component.Images.Count} image(s) ready to build and push.");
        return StepResult.Successful;
    }

    /// <summary>
    /// The registry host a tag names, or null for a bare name that would mean Docker Hub.
    /// </summary>
    internal static string? Registry(string tag) =>
        tag.Split('/') is [var host, _, ..] && (host.Contains('.') || host.Contains(':')) ? host : null;
}
