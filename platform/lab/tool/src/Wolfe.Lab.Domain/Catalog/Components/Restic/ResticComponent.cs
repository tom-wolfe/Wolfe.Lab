using Wolfe.Lab.Domain.Backups;

namespace Wolfe.Lab.Domain.Catalog.Components.Restic;

/// <summary>
/// Where the lab's backups go: the local repository and its offsite copy, what a prune keeps of
/// them, and how much of the copy a check reads back.
/// </summary>
public sealed class ResticComponent : Component
{
    private ResticComponent() { }

    /// <summary>
    /// What the nightly prune keeps.
    /// </summary>
    public required RetentionPolicy Retention { get; init; }

    /// <summary>
    /// The share of the offsite copy's data the weekly check reads back: over a year a small one
    /// covers most of the repository, for pennies.
    /// </summary>
    public Percentage VerifySample { get; set; } = Percentage.From(5);

    /// <summary>
    /// Creates a new restic component, pruned by <paramref name="retention"/>.
    /// </summary>
    public static Result<ResticComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, RetentionPolicy retention)
    {
        var errors = Validate(source, out var directory);
        if (errors.Count != 0)
        {
            return errors;
        }

        return new ResticComponent { Source = source, Directory = directory, Name = name, Kind = kind, Workflow = WorkflowName.Restic, Retention = retention };
    }
}
