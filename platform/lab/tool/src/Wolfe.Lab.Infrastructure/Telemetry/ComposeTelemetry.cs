using Wolfe.Lab.Domain.Telemetry;
using Wolfe.Lab.Infrastructure.Compose;

namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// What a compose project tells the collector: the labels each service sets for it.
/// </summary>
/// <remarks>
/// A compose file sets only the labels meant for it, with values the collector understands, and
/// leaves the deploy's own — where the component lives, and the resource attributes that say so —
/// to the deploy: set here too, one would silently override the other.
/// </remarks>
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
                    errors.Add(new Error($"service '{service.Name}' sets the label {name}, which the deploy sets from where the component lives."));
                }
                else if (ContainerLabel.Named(name).Value is { } label)
                {
                    if (label.Accept(value).Errors is { } refused)
                    {
                        errors.AddRange(refused.Select(error => new Error($"service '{service.Name}': {error.Message}")));
                    }
                    else
                    {
                        own[label] = value;
                    }
                }
                else if (name.StartsWith("lab.", StringComparison.Ordinal) && ContainerLabel.Named(name).Errors is { } unknown)
                {
                    errors.AddRange(unknown.Select(error => new Error($"service '{service.Name}' sets the label {name}: {error.Message}")));
                }
            }

            if (service.Environment.ContainsKey(TelemetryAttribute.ResourceAttributesVariable))
            {
                errors.Add(new Error($"service '{service.Name}' sets {TelemetryAttribute.ResourceAttributesVariable}, which the deploy sets from where the component lives."));
            }

            if (own.Count > 0)
            {
                labels[service.Name] = own;
            }
        }

        return errors.Count > 0 ? errors : new ComposeTelemetry(labels);
    }
}
