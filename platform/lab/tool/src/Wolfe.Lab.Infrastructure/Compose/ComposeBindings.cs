using System.Globalization;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// A directory's compose components bound to its compose stack's services, exactly: every
/// service is one component's, and every component names a service the stack has.
/// </summary>
/// <remarks>
/// The collector runs on the host, which reaches a container only through a port it publishes
/// (on a Mac, the containers are in a VM). A service that publishes its metrics port already is
/// scraped there; one that does not is published on loopback alone, so nothing beyond the node
/// reaches it.
/// </remarks>
/// <param name="Bindings">Each service and its component, by the service's name.</param>
public sealed record ComposeBindings(IReadOnlyList<ComposeBinding> Bindings)
{
    /// <summary>
    /// <paramref name="unit"/>'s compose components bound to <paramref name="project"/>,
    /// or every way they do not fit it — each a component's problem at its declaration, or a
    /// service no component declares.
    /// </summary>
    public static Result<ComposeBindings> Of(DeploymentUnit unit, ComposeProject project)
    {
        var errors = new List<Error>();
        var services = project.Services.ToDictionary(service => service.Name, StringComparer.Ordinal);
        var bound = new Dictionary<string, ComposeBinding>(StringComparer.Ordinal);
        // Claimed by a component even when its binding is refused, so that is said once, not twice.
        var claimed = new Dictionary<string, Component>(StringComparer.Ordinal);
        foreach (var component in unit.Components)
        {
            if (component is not ComposeComponent compose)
            {
                continue;
            }

            var refused = Refusing(component, errors);
            if (!services.TryGetValue(compose.ComposeService.Value, out var service))
            {
                refused("service", ComposeBindingErrors.NotInTheStack(compose.ComposeService, services.Keys));
                continue;
            }

            if (!claimed.TryAdd(service.Name, component))
            {
                refused("service", ComposeBindingErrors.BoundAlready(compose.ComposeService, claimed[service.Name].Name));
                continue;
            }

            if (Bind(compose, service, refused) is { } binding)
            {
                bound[service.Name] = binding;
            }
        }

        errors.AddRange(services.Keys
            .Where(service => !claimed.ContainsKey(service))
            .Order(StringComparer.Ordinal)
            .Select(service => ComposeBindingErrors.Undeclared(service, unit.Directory)));
        return errors.Count > 0 ? errors : new ComposeBindings([.. bound.Values.OrderBy(binding => binding.Service.Name, StringComparer.Ordinal)]);
    }

    /// <summary>
    /// What <paramref name="compose"/>'s facets ask of its service, or null when one cannot be met.
    /// </summary>
    private static ComposeBinding? Bind(ComposeComponent compose, ComposeService service, Action<string, Error> refused)
    {
        var labels = new Dictionary<ContainerLabel, string>();
        var publishes = new List<string>();
        if (compose.Logs is { } logs)
        {
            if (service.Labels.ContainsKey(ContainerLabel.Logs.Name))
            {
                refused("logs", ComposeBindingErrors.LogsDeclaredTwice);
                return null;
            }

            labels[ContainerLabel.Logs] = logs.Value;
        }

        if (compose.Metrics is { } metrics)
        {
            var target = metrics.Port.Value;
            int scraped;
            switch (service.NetworkMode)
            {
                case null or "" or "bridge" or "default":
                    if (service.OnLoopback(target) is { } published)
                    {
                        scraped = published;
                    }
                    else
                    {
                        scraped = target;
                        publishes.Add($"127.0.0.1:{target}:{target}");
                    }

                    break;
                case "host":
                    scraped = target;
                    break;
                default:
                    refused("metrics", ComposeBindingErrors.NoNetworkOfItsOwn(service.NetworkMode));
                    return null;
            }

            labels[ContainerLabel.MetricsPort] = scraped.ToString(CultureInfo.InvariantCulture);
            labels[ContainerLabel.MetricsPath] = metrics.Path.Value;
        }

        return new ComposeBinding(compose, service, labels, publishes);
    }

    // A problem with one field of the component's declaration, reported where it is declared.
    private static Action<string, Error> Refusing(Component component, List<Error> errors) =>
        (field, problem) => errors.Add(CatalogError.In(component.Source, new FieldError(field, problem)));
}
