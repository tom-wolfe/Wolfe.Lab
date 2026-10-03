namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// A part of a service the lab operates, as its declaration says.
/// </summary>
/// <param name="Name">Its name, when the declaration gives one: a component in a directory of its
/// own defaults to the directory's.</param>
/// <param name="DisplayName">The name people read, when the name is not it.</param>
/// <param name="Description">What it is, in a sentence.</param>
/// <param name="Type">What it is: its kind, and its kind's type.</param>
/// <param name="DependsOn">The components of its own service it needs.</param>
public sealed record Component(
    ComponentName? Name,
    string? DisplayName,
    string? Description,
    ComponentType Type,
    IReadOnlyList<ComponentName> DependsOn
);
