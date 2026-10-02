namespace Wolfe.Lab.Domain.Telemetry;

/// <summary>
/// An attribute the lab's telemetry is labelled with (ROADMAP.md #11), in OpenTelemetry's
/// spelling. The one place each is named: what labels a container, a log file or a process's
/// own telemetry reads it from here.
/// </summary>
public sealed record TelemetryAttribute(string Name)
{
    /// <summary>
    /// What emitted it: a container, an agent.
    /// </summary>
    public static TelemetryAttribute ServiceName { get; } = new("service.name");

    /// <summary>
    /// The area the component lives in: <c>monitoring</c>.
    /// </summary>
    public static TelemetryAttribute Area { get; } = new("lab.area");

    /// <summary>
    /// The service the component belongs to: <c>gatus</c>.
    /// </summary>
    public static TelemetryAttribute Service { get; } = new("lab.service");

    /// <summary>
    /// The component itself: <c>compose</c>.
    /// </summary>
    public static TelemetryAttribute Component { get; } = new("lab.component");

    /// <summary>
    /// The node it ran on.
    /// </summary>
    public static TelemetryAttribute HostName { get; } = new("host.name");

    /// <summary>
    /// The node's role — <c>server</c> or <c>hybrid</c>.
    /// </summary>
    public static TelemetryAttribute Role { get; } = new("lab.role");

    /// <summary>
    /// Every attribute the lab's telemetry carries.
    /// </summary>
    public static IReadOnlyList<TelemetryAttribute> All { get; } = [ServiceName, HostName, Area, Service, Component, Role];

    /// <summary>
    /// The variable an OpenTelemetry SDK reads its resource attributes from.
    /// </summary>
    public const string ResourceAttributesVariable = "OTEL_RESOURCE_ATTRIBUTES";

    /// <summary>
    /// The attribute OpenTelemetry spells <paramref name="name"/> — <c>lab.area</c> — or why the
    /// lab has none.
    /// </summary>
    public static Result<TelemetryAttribute> Named(string name) =>
        All.FirstOrDefault(attribute => attribute.Name == name) is { } found
            ? found
            : new Error($"'{name}' is not an attribute the lab knows ({string.Join(", ", All.Select(attribute => attribute.Name))}).");

    /// <summary>
    /// The attribute a store spells <paramref name="label"/> — <c>lab_area</c> — or why the lab
    /// has none.
    /// </summary>
    public static Result<TelemetryAttribute> Labelled(string label) =>
        All.FirstOrDefault(attribute => attribute.Label == label) is { } found
            ? found
            : new Error($"'{label}' is not an attribute the lab knows ({string.Join(", ", All.Select(attribute => attribute.Label))}).");

    /// <summary>
    /// How a store whose label names cannot hold a dot spells it — Loki's and Prometheus's own
    /// translation of OpenTelemetry's, so <c>service.name</c> is <c>service_name</c>.
    /// </summary>
    public string Label => Name.Replace('.', '_');

    /// <inheritdoc />
    public override string ToString() => Name;
}
