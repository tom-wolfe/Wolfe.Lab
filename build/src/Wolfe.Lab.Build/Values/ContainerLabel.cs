namespace Wolfe.Lab.Build.Values;

/// <summary>
/// A Docker label set in the compose file.
/// </summary>
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
}
