using Wolfe.Lab.Domain.Telemetry;

namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// The well-known problems with what a compose file tells the collector (<see cref="ComposeTelemetry"/>).
/// </summary>
public static class ComposeTelemetryErrors
{
    /// <summary>
    /// A service sets a label the deploy sets from where the component lives.
    /// </summary>
    public static Error SetsWhereItLives(string service, string label) =>
        new($"service '{service}' sets the label {label}, which the deploy sets from where the component lives.");

    /// <summary>
    /// A service sets a label the deploy writes from a facet of the component's declaration.
    /// </summary>
    public static Error SetsAFacet(string service, ContainerLabel label) =>
        new($"service '{service}' sets the label {label.Name}, which the deploy sets from the component's {label.Facet} (component.yaml).");

    /// <summary>
    /// A service sets a label the collector reads to a value it does not understand.
    /// </summary>
    public static Error NotUnderstood(string service, ContainerLabel label, string value) =>
        new($"service '{service}': {ContainerLabelErrors.NotUnderstood(label, value).Message}");

    /// <summary>
    /// A service sets a <c>lab.</c> label the lab does not know.
    /// </summary>
    public static Error UnknownLabel(string service, string name) =>
        new($"service '{service}' sets the label {name}: {ContainerLabelErrors.Unknown(name, ContainerLabel.All.Select(label => label.Name)).Message}");

    /// <summary>
    /// A service sets the resource attributes the deploy sets from where the component lives.
    /// </summary>
    public static Error SetsResourceAttributes(string service) =>
        new($"service '{service}' sets {TelemetryAttribute.ResourceAttributesVariable}, which the deploy sets from where the component lives.");
}
