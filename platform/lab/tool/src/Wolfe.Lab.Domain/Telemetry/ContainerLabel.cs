namespace Wolfe.Lab.Domain.Telemetry;

/// <summary>
/// A Docker label set in the compose file to tell the collector what to collect.
/// </summary>
/// <param name="Name">The label as the compose file spells it: <c>lab.logs</c>.</param>
/// <param name="Values">Every value the collector understands.</param>
public sealed record ContainerLabel(string Name, IReadOnlyList<string> Values)
{
    /// <summary>
    /// The container sends its own logs over OTLP, so the collector leaves its stdout alone
    /// rather than storing every line twice.
    /// </summary>
    public static ContainerLabel Logs { get; } = new("lab.logs", ["otlp"]);

    /// <summary>
    /// Every label a compose file may set.
    /// </summary>
    public static IReadOnlyList<ContainerLabel> All { get; } = [Logs];

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
            : new Error($"'{name}' is not a label the lab knows ({string.Join(", ", All.Select(label => label.Name))}).");

    /// <summary>
    /// The label the collector's discovery spells <paramref name="label"/> — <c>lab_logs</c> — or
    /// why the lab has none.
    /// </summary>
    public static Result<ContainerLabel> Labelled(string label) =>
        All.FirstOrDefault(known => known.Label == label) is { } found
            ? found
            : new Error($"'{label}' is not a label the lab knows ({string.Join(", ", All.Select(known => known.Label))}).");

    /// <summary>
    /// <paramref name="value"/>, when it is one the collector understands for this label.
    /// </summary>
    /// <remarks>
    /// <see cref="Result.Success{T}"/> rather than the value alone: a string converts to an
    /// <see cref="Error"/> too, and would read as one.
    /// </remarks>
    public Result<string> Accept(string value) =>
        Values.Contains(value) ? Result.Success(value) : new Error($"{Name} is '{value}': the collector understands {string.Join(", ", Values)}.");
}
