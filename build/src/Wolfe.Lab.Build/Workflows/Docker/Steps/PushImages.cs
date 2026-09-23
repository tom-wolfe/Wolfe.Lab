using Ritten.Docker;
using Wolfe.Lab.Build.Workflows.Docker.Models;

namespace Wolfe.Lab.Build.Workflows.Docker.Steps;

/// <summary>
/// Pushes each built image to the registry its tag names.
/// </summary>
/// <remarks>
/// So a node that did not build an image can still run it: the CI image is built on the Pi, and
/// every containerised runner pulls it from Forgejo's registry by the same name.
/// </remarks>
[Step("push images", StepKind.Publish)]
internal sealed class PushImages(IDocker docker, ISecretProvider secrets, RegistryPush push, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        if (push.Credential is not { } credential || push.Images.Count == 0)
        {
            log.Detail("No registry named; the images stay on this node.");
            return StepResult.Successful;
        }

        var registries = push.Images.Select(image => Registry(image.Tag)).Distinct().ToList();
        if (registries.Contains(null))
        {
            return new Error("Every pushed image's tag must name its registry host, e.g. code.twolfe.dev/owner/name.");
        }

        if (job.DryRun)
        {
            log.Skipped($"Would push {string.Join(", ", push.Images.Select(image => image.Tag))}.");
            return StepResult.Successful;
        }

        var token = await secrets.Resolve(credential.Token.Value, ct);
        foreach (var registry in registries)
        {
            await docker.Login(registry!, credential.Username.Value, token, ct);
        }

        foreach (var image in push.Images)
        {
            await docker.Push(image.Tag, ct);
            log.Status($"Pushed {image.Tag}.");
        }

        return StepResult.Successful;
    }

    /// <summary>
    /// The registry host a tag names, or null for a bare name that would mean Docker Hub.
    /// </summary>
    internal static string? Registry(string tag) =>
        tag.Split('/') is [var host, _, ..] && (host.Contains('.') || host.Contains(':')) ? host : null;
}
