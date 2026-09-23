using Ritten.Docker;

namespace Wolfe.Lab.Build.Workflows.Docker.Models;

/// <summary>
/// What an image build pushes, and as whom.
/// </summary>
/// <param name="Images">The images to push, each to the registry its tag names.</param>
/// <param name="Credential">Who pushes them; null when the component names no registry.</param>
public sealed record RegistryPush(IReadOnlyList<DockerImage> Images, RegistryCredential? Credential)
{
    /// <summary>
    /// A component that names no registry: its images stay on the node that built them.
    /// </summary>
    public static RegistryPush None { get; } = new([], null);
}
