using Ritten.Docker;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Domain.Network;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// A directory's Docker components bound to its compose stack's services, exactly: every
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
    /// <paramref name="unit"/>'s Docker components bound to <paramref name="project"/>,
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
            if (component is not DockerComponent compose)
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
    private static ComposeBinding? Bind(DockerComponent compose, ComposeService service, Action<string, Error> refused)
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

        var targets = new List<MetricsTarget>();
        foreach (var (metrics, index) in compose.Metrics.Select((metrics, index) => (metrics, index)))
        {
            Port scraped;
            switch (service.NetworkMode)
            {
                // A port the file publishes on loopback already is scraped where it is, unless the
                // component chooses another; any other is published on loopback alone.
                case null or "" or "bridge" or "default":
                    if (metrics.Published is null && service.OnLoopback(metrics.Port.Value) is { } published)
                    {
                        scraped = Port.From(published);
                    }
                    else
                    {
                        scraped = metrics.Published ?? metrics.Port;
                        publishes.Add($"127.0.0.1:{scraped.Value}:{metrics.Port.Value}");
                    }

                    break;
                case "host":
                    scraped = metrics.Port;
                    break;
                default:
                    refused(compose.Metrics.Count > 1 ? $"metrics.{index}" : "metrics", ComposeBindingErrors.NoNetworkOfItsOwn(service.NetworkMode));
                    return null;
            }

            targets.Add(new MetricsTarget(scraped, metrics.Path));
        }

        return new ComposeBinding(compose, service, labels, publishes) { Metrics = targets };
    }

    // A problem with one field of the component's declaration, reported where it is declared.
    private static Action<string, Error> Refusing(Component component, List<Error> errors) =>
        (field, problem) => errors.Add(CatalogError.In(component.Source, new FieldError(field, problem)));
}
