namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// What a component is in particular: one of its kind's types — <c>workload</c>/<c>compose</c>.
/// </summary>
/// <param name="Kind">What it is in general.</param>
/// <param name="Name">The type, as a declaration writes it.</param>
public sealed record ComponentType(ComponentKind Kind, ComponentTypeName Name)
{
    /// <summary>
    /// A compose stack.
    /// </summary>
    public static ComponentType Compose { get; } = new(ComponentKind.Workload, ComponentTypeName.From("compose"));

    /// <summary>
    /// Host processes under launchd or systemd.
    /// </summary>
    public static ComponentType Agent { get; } = new(ComponentKind.Workload, ComponentTypeName.From("agent"));

    /// <summary>
    /// A snapshot of a component's state into restic.
    /// </summary>
    public static ComponentType Snapshot { get; } = new(ComponentKind.Backup, ComponentTypeName.From("snapshot"));

    /// <summary>
    /// A directory pushed to a git repository.
    /// </summary>
    public static ComponentType Git { get; } = new(ComponentKind.Backup, ComponentTypeName.From("git"));

    /// <summary>
    /// A runner on the host.
    /// </summary>
    public static ComponentType HostRunner { get; } = new(ComponentKind.Runner, ComponentTypeName.From("host"));

    /// <summary>
    /// A runner in a container.
    /// </summary>
    public static ComponentType DockerRunner { get; } = new(ComponentKind.Runner, ComponentTypeName.From("docker"));

    /// <summary>
    /// A restic repository.
    /// </summary>
    public static ComponentType Restic { get; } = new(ComponentKind.Repository, ComponentTypeName.From("restic"));

    /// <summary>
    /// An OpenTofu root.
    /// </summary>
    public static ComponentType Tofu { get; } = new(ComponentKind.Infrastructure, ComponentTypeName.From("tofu"));

    /// <summary>
    /// A certificate from an ACME issuer.
    /// </summary>
    public static ComponentType Acme { get; } = new(ComponentKind.Certificate, ComponentTypeName.From("acme"));

    /// <summary>
    /// A container image.
    /// </summary>
    public static ComponentType Image { get; } = new(ComponentKind.Package, ComponentTypeName.From("image"));

    /// <summary>
    /// A NuGet package.
    /// </summary>
    public static ComponentType NuGet { get; } = new(ComponentKind.Package, ComponentTypeName.From("nuget"));

    /// <summary>
    /// chezmoi's profiles.
    /// </summary>
    public static ComponentType Chezmoi { get; } = new(ComponentKind.Machine, ComponentTypeName.From("chezmoi"));

    /// <summary>
    /// Every type there is.
    /// </summary>
    public static IReadOnlyList<ComponentType> All { get; } =
        [Compose, Agent, Snapshot, Git, HostRunner, DockerRunner, Restic, Tofu, Acme, Image, NuGet, Chezmoi];

    /// <summary>
    /// The type a declaration names for its kind, or why its kind has none of that name.
    /// </summary>
    public static Result<ComponentType> Of(ComponentKind kind, string name) =>
        All.FirstOrDefault(type => type.Kind == kind && type.Name.Value == name) is { } found
            ? found
            : new Error($"'{name}' is not a type of {kind} ({string.Join(", ", All.Where(type => type.Kind == kind).Select(type => type.Name))}).");

    /// <inheritdoc />
    public override string ToString() => $"{Kind}/{Name}";
}
