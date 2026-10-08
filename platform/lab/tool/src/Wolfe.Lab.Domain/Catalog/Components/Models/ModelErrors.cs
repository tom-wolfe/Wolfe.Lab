namespace Wolfe.Lab.Domain.Catalog.Components.Models;

/// <summary>
/// The well-known problems with a model's declaration.
/// </summary>
public static class ModelErrors
{
    /// <summary>
    /// No server answers it.
    /// </summary>
    public static Error NoServer { get; } = new("doesn't declare any servers to host the model.");

    /// <summary>
    /// A server has no model for it: none of its own, and no default.
    /// </summary>
    public static Error NoModel(ComponentName server) => new($"'{server}' has no model for it: give it one, or give the model a default.");

    /// <summary>
    /// What would serve it is not one of its service's servers.
    /// </summary>
    public static Error NotAServer(ComponentName server) =>
        new($"'{server}' is not one of its service's servers: a component of the service that runs as an agent.");

    /// <summary>
    /// Some of its service's servers do not serve it: a use only some answer would be "not found"
    /// whenever the others are the ones answering, where it should degrade to their best.
    /// </summary>
    public static Error NotServedBy(IReadOnlyList<ComponentName> servers) =>
        new($"is not served by {string.Join(", ", servers)}: every server answers every model, so a use degrades rather than disappears when one is off.");
}
