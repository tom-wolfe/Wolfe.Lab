using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>forgejo-runner</c> workflow operates, as its file writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ForgejoRunnerDocument : ComponentDocument
{
    [Required, MinLength(1), Description("The 1Password vault each runner's registration secret is kept in.")]
    public string Vault { get; init; } = "";

    [Required, Pattern("^[^/\\s]+/[^/\\s]+$"), Description("The repository a host runner is scoped to: tom-wolfe/Wolfe.Lab.")]
    public string Repository { get; init; } = "";

    [Required, MinLength(1), Description("The image a Docker runner's jobs run in by default: node:22-bookworm.")]
    public string Image { get; init; } = "";
}
