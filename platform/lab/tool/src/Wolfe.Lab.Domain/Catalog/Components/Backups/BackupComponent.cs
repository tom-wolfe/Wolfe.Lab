using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog.Components.Backups;

/// <summary>
/// Represents a configured backup.
/// </summary>
public sealed class BackupComponent : Component
{
    private BackupComponent() { }

    /// <summary>
    /// The directories a snapshot holds.
    /// </summary>
    public required IReadOnlyList<HostPath> Paths { get; init; }

    /// <summary>
    /// What it leaves out of them: caches, logs, anything the service regenerates.
    /// </summary>
    public IReadOnlyList<HostPath> Excludes { get; init; } = [];

    /// <summary>
    /// What a restore must bring back non-empty.
    /// </summary>
    public IReadOnlyList<HostPath> Verify { get; init; } = [];

    /// <summary>
    /// Whether it is taken while what it is part of runs: files nothing writes to mid-snapshot,
    /// or a database that keeps its own dumps.
    /// </summary>
    public bool Warm { get; init; }

    /// <summary>
    /// What is stopped while it is taken, if anything: what it is part of, unless it is warm.
    /// </summary>
    public Component? Stops => Warm ? null : Host;

    /// <summary>
    /// Creates a new backup component.
    /// </summary>
    public static Result<BackupComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, ComponentName? partOf, IReadOnlyList<ComponentName> dependsOn,
        IReadOnlyList<HostPath> paths, IReadOnlyList<HostPath> excludes, IReadOnlyList<HostPath> verify, bool warm)
    {
        var errors = Validate(source, name, partOf, dependsOn, out var directory);
        errors.AddRange(Problems(paths, excludes, verify, warm, partOf).Select(Error (problem) => CatalogError.In(source, problem)));
        if (errors.Count != 0)
        {
            return errors;
        }

        return new BackupComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = WorkflowName.Backup,
            PartOf = partOf,
            DependsOn = dependsOn,
            Paths = paths,
            Excludes = excludes,
            Verify = verify,
            Warm = warm
        };
    }

    private static IEnumerable<Error> Problems(IReadOnlyList<HostPath> paths, IReadOnlyList<HostPath> excludes, IReadOnlyList<HostPath> verify, bool warm, ComponentName? partOf)
    {
        if (paths.Count == 0)
        {
            yield return new FieldError("paths", BackupErrors.NothingToSnapshot);
        }

        foreach (var (field, written) in new[] { ("excludes", excludes), ("verify", verify) })
        {
            foreach (var (path, index) in written.Select((path, index) => (path, index)).Where(path => !paths.Any(root => Under(path.path, root))))
            {
                yield return new FieldError($"{field}.{index}", BackupErrors.OutsideThePaths(path));
            }
        }

        if (warm && partOf is null)
        {
            yield return new FieldError("warm", BackupErrors.WarmOfNothing);
        }
    }

    private static bool Under(HostPath path, HostPath root) =>
        path.Value == root.Value || path.Value.StartsWith(root.Value.TrimEnd('/') + "/", StringComparison.Ordinal);
}
