namespace Wolfe.Lab.Domain.Catalog.Components.Models;

/// <summary>
/// What one server runs for a model where it differs from the model's defaults: nothing, when it
/// runs them.
/// </summary>
/// <param name="Model">The model it runs instead.</param>
/// <param name="Context">The context it runs it with instead.</param>
public sealed record ModelServing(ModelTag? Model = null, ContextLength? Context = null);
