using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>image</c> workflow operates, as its file writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ImageDocument : ComponentDocument
{
    [Required, Description("Where the image is pushed, its registry's host first: code.twolfe.dev/tom-wolfe/ci.")]
    public string Tag { get; init; } = "";

    [Description("The build's context, from the component's directory: . when not written.")]
    public string? Context { get; init; }

    [Description("The Dockerfile, from the context: Dockerfile when not written.")]
    public string? Dockerfile { get; init; }
}
