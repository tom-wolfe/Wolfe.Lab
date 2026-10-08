using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// What one server runs for a model, where it differs from the model's defaults.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ServingDocument
{
    [Description("The model this server runs for it, in place of the default.")]
    public string? Model { get; init; }

    [Minimum(1), Description("The context this server runs it with, in tokens, in place of the default.")]
    public int? Context { get; init; }
}
