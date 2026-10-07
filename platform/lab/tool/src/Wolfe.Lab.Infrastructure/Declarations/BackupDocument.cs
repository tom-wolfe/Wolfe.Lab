using Json.Schema.Generation;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// A component the <c>backup</c> workflow operates: state snapshotted into restic.
/// </summary>
[AdditionalProperties(false)]
internal sealed record BackupDocument : ComponentDocument
{
    [Required, MinLength(1), UniqueItems(true), Description("The directories a snapshot holds: absolute, or from ~/.")]
    public List<string> Paths { get; init; } = [];

    [UniqueItems(true), Description("What the snapshot leaves out of them: caches, logs, anything the service regenerates.")]
    public List<string>? Excludes { get; init; }

    [UniqueItems(true), Description("What a restore must bring back non-empty: what proves a snapshot, for the drill.")]
    public List<string>? Verify { get; init; }

    [Description("Taken while what it is part of runs, rather than stopping it: files nothing writes to mid-snapshot, or a database that keeps its own dumps.")]
    public bool? Warm { get; init; }
}
