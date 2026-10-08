using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Services;

namespace Wolfe.Lab.Domain.Catalog.Components.Models;

/// <summary>
/// A model, declared by what it is used for — <c>interactive</c>, <c>embedding</c> — and served
/// by each of its service's servers under the name <c>lab/&lt;name&gt;</c>, with whichever model
/// that server runs for it.
/// </summary>
public sealed class ModelComponent : Component
{
    private ModelComponent() { }

    /// <summary>
    /// Each server that serves it, by the server's name, with what it runs instead of the defaults.
    /// </summary>
    public required IReadOnlyDictionary<ComponentName, ModelServing> ServedBy { get; init; }

    /// <summary>
    /// The model every server runs for it, unless one says otherwise.
    /// </summary>
    public ModelTag? Model { get; set; }

    /// <summary>
    /// The context every server runs it with, unless one says otherwise; the model's own when none says.
    /// </summary>
    public ContextLength? Context { get; set; }

    /// <summary>
    /// The name callers ask for it by, on every server: <c>lab/interactive</c>.
    /// </summary>
    public string Alias => $"lab/{Name}";

    /// <summary>
    /// The model <paramref name="server"/> runs for it: its own, or the default.
    /// </summary>
    public ModelTag? ModelOn(ComponentName server) => ServedBy.GetValueOrDefault(server)?.Model ?? Model;

    /// <summary>
    /// The context <paramref name="server"/> runs it with: its own, or the default.
    /// </summary>
    public ContextLength? ContextOn(ComponentName server) => ServedBy.GetValueOrDefault(server)?.Context ?? Context;

    /// <summary>
    /// Creates a new model component, served by <paramref name="servedBy"/>.
    /// </summary>
    public static Result<ModelComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, IReadOnlyDictionary<ComponentName, ModelServing> servedBy)
    {
        var errors = Validate(source, out var directory);
        if (servedBy.Count == 0)
        {
            errors.Add(CatalogError.In(source, new FieldError("servedBy", ModelErrors.NoServer)));
        }

        if (errors.Count != 0)
        {
            return errors;
        }

        return new ModelComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = WorkflowName.Ollama,
            ServedBy = servedBy
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// Each server it names must be one of the service's, a component it runs as an agent, and
    /// must have a model for it.
    /// </remarks>
    internal override IReadOnlyList<Error> SetService(Service service)
    {
        var problems = new List<Error>();
        foreach (var server in ServedBy.Keys)
        {
            if (service.FindComponent(server) is not AgentComponent)
            {
                problems.Add(new FieldError($"servedBy.{server}", ModelErrors.NotAServer(server)));
            }
            else if (ModelOn(server) is null)
            {
                problems.Add(new FieldError($"servedBy.{server}", ModelErrors.NoModel(server)));
            }
        }

        return problems.Count > 0 ? problems : base.SetService(service);
    }

    /// <summary>
    /// The models of <paramref name="service"/> that some of its servers do not serve: every
    /// server any of them names must serve each.
    /// </summary>
    public static IEnumerable<Error> Unserved(Service service)
    {
        var models = service.Components.OfType<ModelComponent>().ToList();
        var servers = models.SelectMany(model => model.ServedBy.Keys).ToHashSet();
        foreach (var model in models)
        {
            var missing = servers.Where(server => !model.ServedBy.ContainsKey(server)).OrderBy(server => server.Value, StringComparer.Ordinal).ToList();
            if (missing.Count > 0)
            {
                yield return CatalogError.In(model.Source, new FieldError("servedBy", ModelErrors.NotServedBy(missing)));
            }
        }
    }
}
