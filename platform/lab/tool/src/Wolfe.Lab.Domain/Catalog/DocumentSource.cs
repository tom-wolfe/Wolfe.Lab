using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Which document a declaration is: the file, relative to the repository's root, and its place
/// among the file's documents.
/// </summary>
/// <param name="File">The file, relative to the repository's root, with forward slashes.</param>
/// <param name="Index">Which document of the file, from zero.</param>
/// <param name="Documents">How many the file holds.</param>
public sealed record DocumentSource(RepositoryPath File, int Index = 0, int Documents = 1)
{
    /// <summary>
    /// The file, with the document's number when the file holds more than one: <c>vaults.yaml#2</c>.
    /// </summary>
    public override string ToString() => Documents > 1 ? $"{File}#{Index + 1}" : File.Value;

    /// <summary>
    /// The directories the file sits in, below the repository's root.
    /// </summary>
    public IReadOnlyList<string> Directories => File.Directories;
}
