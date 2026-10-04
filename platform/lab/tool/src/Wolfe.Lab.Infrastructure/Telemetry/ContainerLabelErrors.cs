namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// The well-known problems with a label the collector reads (<see cref="ContainerLabel"/>).
/// </summary>
public static class ContainerLabelErrors
{
    /// <summary>
    /// No label the lab knows is spelled so.
    /// </summary>
    public static Error Unknown(string name, IEnumerable<string> known) => new($"'{name}' is not a label the lab knows ({string.Join(", ", known)}).");

    /// <summary>
    /// A value the collector does not understand for the label.
    /// </summary>
    public static Error NotUnderstood(ContainerLabel label, string value) =>
        new($"{label.Name} is '{value}': the collector understands {string.Join(", ", label.Values ?? [])}.");
}
