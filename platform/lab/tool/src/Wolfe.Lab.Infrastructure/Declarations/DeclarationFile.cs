using Wolfe.Lab.Domain.Paths;


namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// One declaration file.
/// </summary>
/// <param name="Path">Its path from the repository's root, with forward slashes.</param>
/// <param name="Text">What it holds.</param>
public sealed record DeclarationFile(RepositoryPath Path, string Text);
