using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Json.Schema.Generation;
using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// The schema every lab declaration is written to: generated from the documents' own types, so
/// there is one truth, and committed (<see cref="Location"/>) so an editor reads it from the
/// checkout, offline.
/// </summary>
/// <remarks>
/// A document is judged by its <c>kind</c>: a branch per kind, each with an identity of its own so
/// its references resolve within it, and only the branch a document's kind selects reports.
/// </remarks>
public static class LabSchema
{
    /// <summary>
    /// A name a service or component can have; <see cref="ServiceName"/> and <see cref="ComponentName"/> say it in words.
    /// </summary>
    internal const string NamePattern = "^[a-z][a-z0-9-]*$";

    /// <summary>
    /// Where the schema is committed, from the repository's root. Every declaration names it in
    /// its first line, relative to itself.
    /// </summary>
    public static RepositoryPath Location { get; } = RepositoryPath.From("platform/lab/schema/lab.schema.json");

    private const string Id = "https://lab.twolfe.dev/schema";

    private static readonly Lazy<JsonSchema> Built = new(Build);

    /// <summary>
    /// The schema.
    /// </summary>
    private static JsonSchema Schema => Built.Value;

    /// <summary>
    /// The schema as it is committed: indented, and with nothing escaped that JSON does not need
    /// escaped, so a diff of it reads.
    /// </summary>
    public static string Json => JsonSerializer.Serialize(Schema, new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    }) + "\n";

    /// <summary>
    /// What is wrong with a document's shape, each problem with the line it is on, or nothing.
    /// </summary>
    public static IReadOnlyList<(int Line, string Problem)> Judge(YamlDocuments.Document document)
    {
        var element = JsonSerializer.SerializeToElement(document.Root);
        var results = Schema.Evaluate(element, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (results.IsValid)
        {
            return [];
        }

        var problems = new List<(int, string)>();
        foreach (var detail in results.Details ?? [])
        {
            var pointer = detail.InstanceLocation.ToString();
            var path = detail.EvaluationPath.ToString();
            // An `if` that does not hold only means the branch is not this document's; and a
            // summary of the problems beneath it says nothing those do not.
            if (detail.Errors is not { Count: > 0 } errors || path.Contains("/if", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var (keyword, message) in errors)
            {
                if (message.StartsWith("Some ", StringComparison.Ordinal) || keyword is "allOf" or "oneOf" or "anyOf" or "if" or "then" or "else")
                {
                    continue;
                }

                problems.Add((document.LineOf(pointer), Describe(pointer, keyword, message, document.Root)));
            }
        }

        return [.. problems.Distinct()];
    }

    /// <summary>
    /// A problem in the words of the field it is in: the schema's own message, but for the two
    /// that say only that a value failed — a pattern, and a key nothing declares.
    /// </summary>
    private static string Describe(string pointer, string keyword, string message, JsonNode? root)
    {
        var segments = pointer.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var at = segments.Length == 0 ? "the document" : string.Join('.', segments);
        var value = segments.Aggregate(root, (node, segment) => node is JsonArray array && int.TryParse(segment, out var index)
            ? array.ElementAtOrDefault(index)
            : node?[segment]);
        return keyword switch
        {
            "pattern" => $"{at}: '{value}' is not a name: lower case, a letter first, then letters, digits and hyphens.",
            "" or "false" => $"{at}: '{segments.LastOrDefault()}' is not something {Kind(root)} declares.",
            "enum" => $"{at}: {OneOf(segments, value, root) ?? message}",
            _ => $"{at}: {message}"
        };
    }

    /// <summary>
    /// What an enumerated field could have been, in the domain's words: a type is one of its
    /// kind's, a kind one of the lab's, a lifecycle or a link's type one of theirs.
    /// </summary>
    private static string? OneOf(string[] segments, JsonNode? value, JsonNode? root)
    {
        var given = value?.ToString() ?? "";
        return segments switch
        {
            ["type"] when ComponentKind.TryFrom(root?["kind"]?.ToString() ?? "") is { IsSuccess: true } kind
                => ComponentType.Of(kind.ValueObject, given).Errors?.FirstOrDefault()?.Message,
            ["kind"] => $"'{given}' is not a kind of declaration (service, {Listed<ComponentKind>()}).",
            ["lifecycle"] => $"'{given}' is not a lifecycle ({Listed<Lifecycle>()}).",
            ["links", _, "type"] => $"'{given}' is not a type of link ({Listed<LinkType>()}).",
            _ => null
        };
    }

    private static string Listed<T>() where T : IClosedSet<T> => string.Join(", ", T.All);

    private static string Kind(JsonNode? root) => root?["kind"]?.GetValue<string>() is { } kind ? $"a {kind}" : "a declaration";

    private static JsonSchema Build()
    {
        var configuration = new SchemaGeneratorConfiguration
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase,
            PropertyOrder = PropertyOrder.AsDeclared
        };
        configuration.Generators.Add(ClosedSetSchemas.Instance);
        var service = new JsonSchemaBuilder().Id($"{Id}/service").FromType<ServiceDocument>(configuration);
        var component = new JsonSchemaBuilder().Id($"{Id}/component").FromType<ComponentDocument>(configuration);
        var kinds = ComponentKind.All;

        static JsonSchemaBuilder KindIs(params string[] kinds) => new JsonSchemaBuilder()
            .Properties(("kind", kinds.Length == 1 ? new JsonSchemaBuilder().Const(kinds[0]) : new JsonSchemaBuilder().Enum(kinds)));

        return new JsonSchemaBuilder()
            .Schema(MetaSchemas.Draft202012Id)
            .Id(Id)
            .Title("Wolfe.Lab declaration")
            .Description("A service or a component of the lab (ROADMAP.md #14). Name this schema in a file's first line to make it one of the lab's.")
            .Type(SchemaValueType.Object)
            .Required("kind")
            .Properties(("kind", new JsonSchemaBuilder()
                .Description("What the document declares: a service, or a kind of component.")
                .Enum(["service", .. kinds.Select(kind => kind.Value)])))
            .AllOf([
                new JsonSchemaBuilder().If(KindIs("service")).Then(service),
                new JsonSchemaBuilder().If(KindIs([.. kinds.Select(kind => kind.Value)])).Then(component),
                .. kinds.Select(kind => new JsonSchemaBuilder()
                    .If(KindIs(kind.Value))
                    .Then(new JsonSchemaBuilder().Properties(("type", new JsonSchemaBuilder()
                        .Enum(ComponentType.All.Where(type => type.Kind == kind).Select(type => type.Name.Value))))))
            ])
            .Build();
    }
}
