namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// A Docker label that tells the collector what to collect from a container.
/// </summary>
/// <param name="Name">The label as Docker spells it: <c>lab.logs</c>.</param>
/// <param name="Values">Every value the collector understands, or null when it takes any.</param>
/// <param name="Facet">The facet of a component's declaration the deploy writes it from, which a
/// compose file leaves it to; null for one a compose file may still set.</param>
public sealed record ContainerLabel(string Name, IReadOnlyList<string>? Values, string? Facet = null)
{
    /// <summary>
    /// The container sends its own logs over OTLP, so the collector leaves its stdout alone
    /// rather than storing every line twice.
    /// </summary>
    /// <remarks>
    /// A compose file may set it still, until each one is declared in its component's
    /// <c>logs</c> instead; never both.
    /// </remarks>
    public static ContainerLabel Logs { get; } = new("lab.logs", ["otlp"]);

    /// <summary>
    /// The host's loopback port the collector scrapes the container's metrics on.
    /// </summary>
    public static ContainerLabel MetricsPort { get; } = new("lab.metrics.port", null, "metrics");

    /// <summary>
    /// The path the collector scrapes them at.
    /// </summary>
    public static ContainerLabel MetricsPath { get; } = new("lab.metrics.path", null, "metrics");

    /// <summary>
    /// Every label the collector reads.
    /// </summary>
    public static IReadOnlyList<ContainerLabel> All { get; } = [Logs, MetricsPort, MetricsPath];

    /// <summary>
    /// How the collector sees it once Docker's discovery has spelled it: no dots.
    /// </summary>
    public string Label => Name.Replace('.', '_');

    /// <summary>
    /// The label a compose file spells <paramref name="name"/>, or why the lab has none.
    /// </summary>
    public static Result<ContainerLabel> Named(string name) =>
        All.FirstOrDefault(label => label.Name == name) is { } found
            ? found
            : ContainerLabelErrors.Unknown(name, All.Select(label => label.Name));

    /// <summary>
    /// The label the collector's discovery spells <paramref name="label"/> — <c>lab_logs</c> — or
    /// why the lab has none.
    /// </summary>
    public static Result<ContainerLabel> Labelled(string label) =>
        All.FirstOrDefault(known => known.Label == label) is { } found
            ? found
            : ContainerLabelErrors.Unknown(label, All.Select(known => known.Label));

    /// <summary>
    /// Whether <paramref name="value"/> is one the collector understands for this label.
    /// </summary>
    public bool Understands(string value) => Values is null || Values.Contains(value);
}
