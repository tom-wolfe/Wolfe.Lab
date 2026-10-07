namespace Wolfe.Lab.Infrastructure.Releases;

/// <summary>
/// The artifacts a job publishes: what the component declared, and whether the deployment's own
/// directory is installed first — compose runs its stack from that copy.
/// </summary>
/// <param name="Artifacts">The declarations.</param>
/// <param name="InstallsUnit">Whether the deployment is itself installed, as the first artifact.</param>
public sealed record ArtifactDeclarations(IReadOnlyList<ArtifactOptions> Artifacts, bool InstallsUnit = false);
