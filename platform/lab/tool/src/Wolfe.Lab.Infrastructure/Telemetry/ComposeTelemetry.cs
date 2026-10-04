using Wolfe.Lab.Domain.Telemetry;
using Wolfe.Lab.Infrastructure.Compose;

namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// What a compose project tells the collector: the labels each service sets for it.
/// </summary>
/// <param name="Labels">The collector's labels and their values, by service; a service that sets none is absent.</param>
public sealed record ComposeTelemetry(IReadOnlyDictionary<string, IReadOnlyDictionary<ContainerLabel, string>> Labels)
{
    /// <summary>
    /// The project's labels for the collector, or everything about them the lab refuses.
    /// </summary>
    public static Result<ComposeTelemetry> From(ComposeProject project)
    {
        var errors = new List<Error>();
        var labels = new Dictionary<string, IReadOnlyDictionary<ContainerLabel, string>>(StringComparer.Ordinal);
        foreach (var service in project.Services)
        {
            var own = new Dictionary<ContainerLabel, string>();
            foreach (var (name, value) in service.Labels.OrderBy(label => label.Key, StringComparer.Ordinal))
            {
                if (TelemetryAttribute.Named(name).IsSuccess)
                {
                    errors.Add(ComposeTelemetryErrors.SetsWhereItLives(service.Name, name));
                }
                else if (ContainerLabel.Named(name).Value is { Facet: not null } written)
                {
                    errors.Add(ComposeTelemetryErrors.SetsAFacet(service.Name, written));
                }
                else if (ContainerLabel.Named(name).Value is { } label)
                {
                    if (!label.Understands(value))
                    {
                        errors.Add(ComposeTelemetryErrors.NotUnderstood(service.Name, label, value));
                    }
                    else
                    {
                        own[label] = value;
                    }
                }
                else if (name.StartsWith("lab.", StringComparison.Ordinal))
                {
                    errors.Add(ComposeTelemetryErrors.UnknownLabel(service.Name, name));
                }
            }

            if (service.Environment.ContainsKey(TelemetryAttribute.ResourceAttributesVariable))
            {
                errors.Add(ComposeTelemetryErrors.SetsResourceAttributes(service.Name));
            }

            if (own.Count > 0)
            {
                labels[service.Name] = own;
            }
        }

        return errors.Count > 0 ? errors : new ComposeTelemetry(labels);
    }
}
