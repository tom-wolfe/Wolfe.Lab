using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// The well-known problems with a declaration file before the domain reads it: its YAML, where it
/// is, the schema it names, and its shape.
/// </summary>
public static class DeclarationErrors
{
    /// <summary>
    /// The file is not YAML.
    /// </summary>
    public static Error NotYaml(long line, string reason) => new($"line {line}: not YAML: {reason}");

    /// <summary>
    /// The file is deeper than a declaration may be.
    /// </summary>
    public static Error TooDeep(int depth) => new($"a declaration is at most {depth} directories deep, area/service/component/.");

    /// <summary>
    /// The file's first line names the schema by a path that does not lead to it.
    /// </summary>
    public static Error SchemaElsewhere(RepositoryPath? named, RepositoryPath schema, string fromHere) =>
        new($"names the schema at {named?.Value ?? "a path out of the repository"}, but it is at {schema}; from here that is {fromHere}.");

    /// <summary>
    /// A value that should be a name is not one.
    /// </summary>
    public static Error NotAName(string? given) => new($"'{given}' is not a name: lower case, a letter first, then letters, digits and hyphens.");

    /// <summary>
    /// A key nothing of the document's kind declares.
    /// </summary>
    public static Error NotDeclarable(string? key, string declaration) => new($"'{key}' is not something {declaration} declares.");

    /// <summary>
    /// A value that is not one of the closed set its field takes.
    /// </summary>
    public static Error NotOneOf(string given, string what, IEnumerable<string> options) => new($"'{given}' is not {what} ({string.Join(", ", options)}).");

    /// <summary>
    /// What the schema says is wrong, in its own words, when the lab has none better.
    /// </summary>
    public static Error Schema(string message) => new(message);

    /// <summary>
    /// A component declares some of its agent, but not what every agent needs.
    /// </summary>
    public static Error AgentIncomplete { get; } =
        new("an agent is declared with its runsOn, agent and program together — and an agents component always declares one.");

}
