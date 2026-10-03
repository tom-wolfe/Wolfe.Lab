using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog.Components;

/// <summary>
/// The well-known problems with a component's declaration, and with finding one.
/// </summary>
public static class ComponentErrors
{
    /// <summary>
    /// The declaration is neither beside its service's entry nor in a directory of its own within it.
    /// </summary>
    public static Error OutOfPlace { get; } = new("a component is declared in its service's directory, or in a directory of its own within it.");

    /// <summary>
    /// The directory it would belong to declares no service that holds.
    /// </summary>
    public static Error NoService(RepositoryPath service) => new($"a component belongs to a service, and {service}/ declares none that holds.");

    /// <summary>
    /// Its service has a component of its name already.
    /// </summary>
    public static Error DeclaredAlready(ComponentName name, DocumentSource other) => new($"'{name}' is declared already, in {other}.");

    /// <summary>
    /// It depends on itself.
    /// </summary>
    public static Error DependsOnItself(ComponentName name) => new($"'{name}' depends on itself.");

    /// <summary>
    /// It depends on a component its service does not declare.
    /// </summary>
    public static Error DependsOnUndeclared(ComponentName needed) =>
        new($"depends on '{needed}', which its service does not declare, or which depends on this one in turn; a component depends only on components of its own service.");

    /// <summary>
    /// Not a kind of component (<see cref="ComponentKind"/>).
    /// </summary>
    public static Error NotAKind(string given) => new($"'{given}' is not a kind of component ({string.Join(", ", ComponentKind.All)}).");

    /// <summary>
    /// Not a workflow that operates components (<see cref="WorkflowName"/>).
    /// </summary>
    public static Error NotAWorkflow(string given) => new($"'{given}' is not a workflow that operates components ({string.Join(", ", WorkflowName.All)}).");

    /// <summary>
    /// Not a name compose gives a service.
    /// </summary>
    public static Error NotAComposeService(string given) =>
        new($"'{given}' is not a compose service's name: a letter or digit first, then letters, digits, '_', '.' and '-'.");

    /// <summary>
    /// It is part of a component its service does not declare.
    /// </summary>
    public static Error PartOfUndeclared(ComponentName whole) =>
        new($"is part of '{whole}', which its service does not declare, or which is part of this one in turn; a component is part only of a component of its own service.");

    /// <summary>
    /// It is part of itself.
    /// </summary>
    public static Error PartOfItself(ComponentName name) => new($"'{name}' is part of itself.");
}
