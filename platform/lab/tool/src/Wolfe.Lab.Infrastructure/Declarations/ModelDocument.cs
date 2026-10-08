using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>ollama</c> workflow operates: a model, by what it is used for, and the
/// servers that serve it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ModelDocument : ComponentDocument
{
    [Description("The model every server runs for it, unless one says otherwise: <model>:<tag>, as the server names it.")]
    public string? Model { get; init; }

    [Minimum(1), Description("The context every server runs it with, in tokens, unless one says otherwise; the server's own default when none says.")]
    public int? Context { get; init; }

    [Required, Description("Each server that serves it, by its component's name, with what it runs otherwise, if anything: every server of the service serves every model.")]
    public Dictionary<string, ServingDocument?> ServedBy { get; init; } = [];
}
