using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>restic</c> workflow operates: the repositories the lab's backups go to.
/// </summary>
[AdditionalProperties(false)]
internal sealed record ResticDocument : ComponentDocument
{
    [Required, Description("What the nightly prune keeps, of both repositories alike.")]
    public RetentionDocument Retention { get; init; } = new();

    [Description("How the weekly check reads the offsite copy back.")]
    public VerifyDocument? Verify { get; init; }
}
