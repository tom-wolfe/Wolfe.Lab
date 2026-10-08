using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// lego, as a certificate component writes it.
/// </summary>
[AdditionalProperties(false)]
internal sealed record IssuerDocument
{
    [Required, MinLength(1), Description("The lego image it runs: goacme/lego:v5.4.0.")]
    public string Image { get; init; } = "";

    [Required, MinLength(1), Description("The ACME account's address.")]
    public string Email { get; init; } = "";

    [Required, MinLength(1), Description("The DNS provider the challenge is answered through, as lego names it: netlify.")]
    public string Dns { get; init; } = "";

    [Required, Description("Where lego keeps its account and the certificates on the node: absolute, or from ~/.")]
    public string Store { get; init; } = "";

    [Description("The provider's credentials, each a variable lego reads and an op:// reference to it.")]
    public Dictionary<string, string>? Environment { get; init; }

    [Description("How long lego waits for the challenge record to propagate, as lego spells it: 90s.")]
    public string? PropagationWait { get; init; }
}
