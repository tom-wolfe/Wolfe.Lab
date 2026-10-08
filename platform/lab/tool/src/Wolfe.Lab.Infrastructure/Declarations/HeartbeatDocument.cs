using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// The healthchecks.io check a component pings when its work is done.
/// </summary>
[AdditionalProperties(false)]
internal sealed record HeartbeatDocument
{
    [Required, Pattern(LabSchema.NamePattern), Description("The check's slug, as its URL names it.")]
    public string Check { get; init; } = "";

    [Required, Pattern("^op://[^/]+/[^/]+/[^/]+$"), Description("Where the account's ping key is: op://<vault>/<item>/<field>.")]
    public string Key { get; init; } = "";
}
