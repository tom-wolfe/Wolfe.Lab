using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Telemetry;

namespace Wolfe.Lab.Infrastructure.Compose;

/// <summary>
/// The well-known ways a directory's Docker components and its compose stack disagree
/// (<see cref="ComposeBindings"/>).
/// </summary>
public static class ComposeBindingErrors
{
    /// <summary>
    /// A component names a service the stack has not got.
    /// </summary>
    public static Error NotInTheStack(ComposeServiceName service, IEnumerable<string> services) =>
        new($"the compose stack has no service '{service}' ({string.Join(", ", services.Order(StringComparer.Ordinal))}).");

    /// <summary>
    /// Two components name the same service.
    /// </summary>
    public static Error BoundAlready(ComposeServiceName service, ComponentName other) => new($"'{service}' is component '{other}''s already.");

    /// <summary>
    /// A service of the stack is no component's.
    /// </summary>
    public static Error Undeclared(string service, RepositoryPath directory) =>
        new($"the compose stack's service '{service}' is no component's: declare one in {directory}, with service: {service}.");

    /// <summary>
    /// The component's logs are declared by it and by the compose file's label too.
    /// </summary>
    public static Error LogsDeclaredTwice { get; } =
        new($"the component's logs are declared twice, here and by the compose file's {ContainerLabel.Logs.Name} label; the label goes.");

    /// <summary>
    /// The service runs in another's network, so it has no ports of its own to be scraped on.
    /// </summary>
    public static Error NoNetworkOfItsOwn(string networkMode) =>
        new($"its service runs in {networkMode}'s network, so it has no ports of its own to be scraped on; declare them on the component whose network it is.");
}
