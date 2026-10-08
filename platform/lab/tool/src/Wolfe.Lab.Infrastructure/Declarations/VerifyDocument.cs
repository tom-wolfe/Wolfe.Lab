using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// How a check reads a repository back.
/// </summary>
[AdditionalProperties(false)]
internal sealed record VerifyDocument
{
    [Required, Pattern("^[0-9]{1,3}%$"), Description("The share of the offsite copy's data read back, as restic spells it: 5%.")]
    public string ReadDataSubset { get; init; } = "";
}
