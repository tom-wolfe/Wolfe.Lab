using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>obsidian</c> workflow operates, as its file writes it: one vault.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ObsidianDocument : ComponentDocument
{
    [Required, Description("Where the vault is checked out on the node: absolute, or from ~/.")]
    public string Path { get; init; } = "";

    [Required, Description("The git repository it is pushed to.")]
    public string Repository { get; init; } = "";

    [Required, Description("What the push authenticates with.")]
    public PushDocument Push { get; init; } = new();

    [UniqueItems(true), Description("What a commit leaves out, as git's exclude patterns: .DS_Store, .trash/.")]
    public List<string>? Exclude { get; init; }
}
