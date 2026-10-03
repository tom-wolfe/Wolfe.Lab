namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Describes a problem with one field of a declaration.
/// </summary>
/// <param name="Field">The field, as a dotted path from the document's root.</param>
/// <param name="Problem">What is wrong with it.</param>
public sealed record FieldError(string Field, Error Problem) : Error($"{Field}: {Problem.Message}", null);
