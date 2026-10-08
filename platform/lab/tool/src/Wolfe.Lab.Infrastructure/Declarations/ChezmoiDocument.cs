using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>chezmoi</c> workflow operates, as its file writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ChezmoiDocument : ComponentDocument
{
    [Required, MinLength(1), UniqueItems(true), Description("The profiles a check renders the source for, each a machine chezmoi is applied to.")]
    public List<string> Profiles { get; init; } = [];
}
