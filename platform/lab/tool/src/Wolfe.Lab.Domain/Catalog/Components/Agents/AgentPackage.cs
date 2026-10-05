using Wolfe.Lab.Domain.Packages;

namespace Wolfe.Lab.Domain.Catalog.Components.Agents;

/// <summary>
/// Defines where an agent's binary comes from.
/// </summary>
/// <param name="Github">The repository: <c>grafana/alloy</c>.</param>
/// <param name="Version">The pinned package version.</param>
/// <param name="Asset">The release's asset to install.</param>
/// <param name="Checksums">The release's checksum file, or null for GitHub's own digest.</param>
public sealed record AgentPackage(GitHubRepository Github, PackageVersion Version, Template Asset, Template? Checksums);
