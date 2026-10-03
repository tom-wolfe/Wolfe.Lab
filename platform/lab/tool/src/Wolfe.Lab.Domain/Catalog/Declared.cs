namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// A declaration and the document it came from.
/// </summary>
/// <typeparam name="T">What is declared.</typeparam>
/// <param name="Declaration">What it says.</param>
/// <param name="Source">Where it says it.</param>
public sealed record Declared<T>(T Declaration, DocumentSource Source);
