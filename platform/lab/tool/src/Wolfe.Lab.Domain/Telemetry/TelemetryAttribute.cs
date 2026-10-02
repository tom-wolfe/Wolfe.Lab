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
    /// Whether <paramref name="label"/> is one of the attributes in a store's spelling.
    /// </summary>
    public static bool IsLabel(string label) => All.Any(attribute => attribute.Label == label);

    /// <summary>
    /// Whether <paramref name="name"/> is one of the attributes in OpenTelemetry's spelling.
    /// </summary>
    public static bool IsName(string name) => All.Any(attribute => attribute.Name == name);

    /// <summary>
    /// How a store whose label names cannot hold a dot spells it — Loki's and Prometheus's own
    /// translation of OpenTelemetry's, so <c>service.name</c> is <c>service_name</c>.
    /// </summary>
    public string Label => Name.Replace('.', '_');

    /// <inheritdoc />
    public override string ToString() => Name;
}
