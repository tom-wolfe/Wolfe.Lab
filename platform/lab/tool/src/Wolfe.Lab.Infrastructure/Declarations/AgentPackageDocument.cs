using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// An agent's package, as its component's file writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record AgentPackageDocument
{
    [Required, Pattern("^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$"), Description("The GitHub repository it is released from: owner/name.")]
    public string Github { get; init; } = "";

    [Required, MinLength(1), Description("The version, pinned: {version} in the asset and checksums.")]
    public string Version { get; init; } = "";

    [Required, MinLength(1), Description("The release's asset to install, named for the node by {platform}, or {platform.os} and {platform.arch}.")]
    public string Asset { get; init; } = "";

    [MinLength(1), Description("The release's checksum file; without one, GitHub's own digest of the asset.")]
    public string? Checksums { get; init; }
}
