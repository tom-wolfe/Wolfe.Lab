using Wolfe.Lab.Domain.Telemetry;

namespace Wolfe.Lab.Domain.Components;

/// <summary>
/// Encapsulates the metadata for a given component.
/// </summary>
public sealed record Component(string Area, string Service, string Name)
{
    /// <summary>
    /// Where the component lives, as the attributes its telemetry is labelled with.
    /// </summary>
    public IReadOnlyList<KeyValuePair<TelemetryAttribute, string>> Attributes =>
    [
        new(TelemetryAttribute.Area, Area),
        new(TelemetryAttribute.Service, Service),
        new(TelemetryAttribute.Component, Name)
    ];

    /// <summary>
    /// The attributes as OpenTelemetry's <c>OTEL_RESOURCE_ATTRIBUTES</c> spells them.
    /// </summary>
    public string ResourceAttributes => string.Join(',', Attributes.Select(attribute => $"{attribute.Key.Name}={attribute.Value}"));

    /// <summary>
    /// The placement as one name, for a file of the component's own: <c>ai-ollama-server</c>.
    /// </summary>
    public string QualifiedName => $"{Area}-{Service}-{Name}";

    /// <summary>
    /// The component a directory is, from its path below the checkout's root —
    /// <c>area/service/component</c> — or null when it is not three levels down.
    /// </summary>
    /// <param name="root">The checkout's root, as an absolute path.</param>
    /// <param name="component">The component's directory, as an absolute path.</param>
    public static Component? From(string root, string component) =>
        Path.GetRelativePath(root, component).Split(Path.DirectorySeparatorChar) is [var area, var service, var name]
        && area is not ".." and not "."
            ? new Component(area, service, name)
            : null;

    /// <inheritdoc />
    public override string ToString() => $"{Area}/{Service}/{Name}";
}
