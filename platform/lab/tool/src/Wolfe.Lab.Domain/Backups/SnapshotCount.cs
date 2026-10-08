namespace Wolfe.Lab.Domain.Backups;

/// <summary>
/// How many snapshots of a kind a prune keeps: none, or some.
/// </summary>
[ValueObject<int>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct SnapshotCount
{
    private static Validation Validate(int input) =>
        input >= 0 ? Validation.Ok : Validation.Invalid($"{input} is no count of snapshots: none, or more.");
}
