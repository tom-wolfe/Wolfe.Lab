using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A push's credential, as a vault writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record PushDocument
{
    [Required, MinLength(1), Description("The account the push authenticates as.")]
    public string Username { get; init; } = "";

    [Required, Description("An op:// reference to the token it pushes with.")]
    public string Token { get; init; } = "";
}
