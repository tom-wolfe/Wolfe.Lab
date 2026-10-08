using Wolfe.Lab.Domain.Catalog.Services;
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
    public IReadOnlyList<HostPath> Excludes { get; set; } = [];

    /// <summary>
    /// What a restore must bring back non-empty.
    /// </summary>
    public IReadOnlyList<HostPath> Verify { get; set; } = [];

    /// <summary>
    /// Whether it is taken while what it is part of runs: files nothing writes to mid-snapshot,
    /// or a database that keeps its own dumps.
    /// </summary>
    public bool Warm { get; set; }

    /// <summary>
    /// What is stopped while it is taken, if anything: what it is part of, unless it is warm.
    /// </summary>
    public Component? Stops => Warm ? null : Host;

    /// <summary>
    /// Creates a new backup component of <paramref name="paths"/>.
    /// </summary>
    public static Result<BackupComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, IReadOnlyList<HostPath> paths)
    {
        var errors = Validate(source, out var directory);
        if (paths.Count == 0)
        {
            errors.Add(CatalogError.In(source, new FieldError("paths", BackupErrors.NothingToSnapshot)));
        }

        if (errors.Count != 0)
        {
            return errors;
        }

        return new BackupComponent { Source = source, Directory = directory, Name = name, Kind = kind, Workflow = WorkflowName.Backup, Paths = paths };
    }

    /// <inheritdoc />
    /// <remarks>
    /// What it leaves out or verifies must lie in what it snapshots, and only a backup part of
    /// something is taken warm.
    /// </remarks>
    internal override IReadOnlyList<Error> SetService(Service service)
    {
        var problems = new List<Error>();
        foreach (var (field, written) in new[] { ("excludes", Excludes), ("verify", Verify) })
        {
            problems.AddRange(written.Select((path, index) => (path, index))
                .Where(path => !Paths.Any(root => Under(path.path, root)))
                .Select(Error (path) => new FieldError($"{field}.{path.index}", BackupErrors.OutsideThePaths(path.path))));
        }

        if (Warm && PartOf is null)
        {
            problems.Add(new FieldError("warm", BackupErrors.WarmOfNothing));
        }

        return problems.Count > 0 ? problems : base.SetService(service);
    }

    private static bool Under(HostPath path, HostPath root) =>
        path.Value == root.Value || path.Value.StartsWith(root.Value.TrimEnd('/') + "/", StringComparison.Ordinal);
}
