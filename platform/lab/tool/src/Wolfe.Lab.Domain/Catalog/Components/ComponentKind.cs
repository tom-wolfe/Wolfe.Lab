namespace Wolfe.Lab.Domain.Catalog.Components;

/// <summary>
/// What a component is used for: the icon it would have on a diagram, independent of its type.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
[Instance("App", "app", "What people use: Immich's server, Jellyfin, Sonarr, Grafana.")]
[Instance("Backend", "backend", "A service's part with no face of its own: the mail watcher, the bridge.")]
[Instance("Database", "database", "What keeps structured data: Postgres, SQLite.")]
[Instance("Cache", "cache", "What keeps data for speed, and could lose it.")]
[Instance("Queue", "queue", "What carries work from one part to another.")]
[Instance("Storage", "storage", "What keeps files: a directory of state, an object store.")]
[Instance("Proxy", "proxy", "What stands in front of others: Caddy.")]
[Instance("Network", "network", "What carries others' traffic: gluetun, a tailnet node.")]
[Instance("Model", "model", "What runs a model: Ollama, Immich's machine learning.")]
[Instance("Collector", "collector", "What gathers telemetry: Alloy, Beszel's agent.")]
[Instance("Runner", "runner", "A CI runner.")]
[Instance("Backup", "backup", "Something that is only a backup: an Obsidian vault.")]
[Instance("Repository", "repository", "Where backups go: a restic repository.")]
[Instance("Infrastructure", "infrastructure", "Resources declared to an API: a tofu root.")]
[Instance("Certificate", "certificate", "A certificate, kept renewed.")]
[Instance("Package", "package", "Something built and published: an image, a tool.")]
[Instance("Machine", "machine", "The nodes' profiles: the kernel's.")]
public readonly partial struct ComponentKind : IClosedSet<ComponentKind>
{
    /// <inheritdoc />
    public static IReadOnlyList<ComponentKind> All =>
    [
        App, Backend, Database, Cache, Queue, Storage, Proxy, Network, Model, Collector,
        Runner, Backup, Repository, Infrastructure, Certificate, Package, Machine
    ];

    private static Validation Validate(string input) =>
        All.Any(kind => kind.Value == input)
            ? Validation.Ok
            : Validation.Invalid(ComponentErrors.NotAKind(input).Message);
}
