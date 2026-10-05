using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Json.Schema.Generation;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Facets.Telemetry;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Catalog.Services;
using Wolfe.Lab.Domain.Paths;

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
    public static IReadOnlyList<(int Line, Error Problem)> Judge(YamlDocuments.Document document)
    {
        var element = JsonSerializer.SerializeToElement(document.Root);
        var results = Schema.Evaluate(element, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (results.IsValid)
        {
            return [];
        }

        // A value one of an anyOf's shapes takes is the right shape: the other shapes' complaints
        // about it are only what it is not. Each shape is reported as …/anyOf/<n>.
        var matched = (results.Details ?? [])
            .Where(detail => detail.IsValid)
            .Select(detail => detail.EvaluationPath.ToString())
            .Select(path => path.LastIndexOf("/anyOf/", StringComparison.Ordinal) is var at and >= 0 && int.TryParse(path[(at + "/anyOf/".Length)..], out _)
                ? path[..(at + "/anyOf/".Length)]
                : null)
            .OfType<string>()
            .ToList();

        var problems = new List<(int, Error)>();
        foreach (var detail in results.Details ?? [])
        {
            var pointer = detail.InstanceLocation.ToString();
            var path = detail.EvaluationPath.ToString();
            // An `if` that does not hold only means the branch is not this document's; and a
            // summary of the problems beneath it says nothing those do not.
            if (detail.Errors is not { Count: > 0 } errors || path.Contains("/if", StringComparison.Ordinal)
                || matched.Any(anyOf => path.StartsWith(anyOf, StringComparison.Ordinal)))
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
    private static Error Describe(string pointer, string keyword, string message, JsonNode? root)
    {
        var segments = pointer.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var value = segments.Aggregate(root, (node, segment) => node is JsonArray array && int.TryParse(segment, out var index)
            ? array.ElementAtOrDefault(index)
            : node?[segment]);
        var problem = keyword switch
        {
            "pattern" => DeclarationErrors.NotAName(value?.ToString()),
            "" or "false" => DeclarationErrors.NotDeclarable(segments.LastOrDefault(), Kind(root)),
            "enum" => OneOf(segments, value, root) ?? DeclarationErrors.Schema(message),
            _ => DeclarationErrors.Schema(message)
        };
        return segments.Length == 0 ? problem : new FieldError(string.Join('.', segments), problem);
    }

    /// <summary>
    /// What an enumerated field could have been, in the domain's words: a kind, a workflow, a
    /// lifecycle, a link's type or a log's delivery, each one of its own closed set.
    /// </summary>
    private static Error? OneOf(string[] segments, JsonNode? value, JsonNode? root)
    {
        var given = value?.ToString() ?? "";
        return segments switch
        {
            ["workflow"] => DeclarationErrors.NotOneOf(given, "a workflow that operates components", Listed<WorkflowName>()),
            ["kind"] => DeclarationErrors.NotOneOf(given, "a kind of declaration", ["service", "node", .. Listed<ComponentKind>()]),
            ["role"] => DeclarationErrors.NotOneOf(given, "a node's role", Listed<NodeRole>()),
            ["platform"] => DeclarationErrors.NotOneOf(given, "a platform", Listed<NodePlatform>()),
            ["lifecycle"] => DeclarationErrors.NotOneOf(given, "a lifecycle", Listed<Lifecycle>()),
            ["links", _, "type"] => DeclarationErrors.NotOneOf(given, "a type of link", Listed<LinkType>()),
            ["logs"] => DeclarationErrors.NotOneOf(given, "a way logs are delivered", Listed<LogTransport>()),
            _ => null
        };
    }

    private static IEnumerable<string> Listed<T>() where T : IClosedSet<T> => T.All.Select(value => value?.ToString() ?? "");

    // A component's shape is its workflow's, so what it may declare is too.
    private static string Kind(JsonNode? root) => (root?["kind"]?.ToString(), root?["workflow"]?.ToString()) switch
    {
        ("service", _) => "a service",
        ("node", _) => "a node",
        (not null, { } workflow) => $"a {workflow} component",
        _ => "a declaration"
    };

    // The shape's name in its branch's identity: compose for ComposeDocument, the shared one common.
    private static string Shape(Type document) =>
        document == typeof(ComponentDocument) ? "common" : document.Name.Replace("Document", "", StringComparison.Ordinal).ToLowerInvariant();

    private static JsonSchema Build()
    {
        var configuration = new SchemaGeneratorConfiguration
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase,
            PropertyOrder = PropertyOrder.AsDeclared
        };
        configuration.Generators.Add(ClosedSetSchemas.Instance);
        configuration.Generators.Add(DeploymentTargetDocument.Schemas.Instance);
        var service = new JsonSchemaBuilder().Id($"{Id}/service").FromType<ServiceDocument>(configuration);
        var node = new JsonSchemaBuilder().Id($"{Id}/node").FromType<NodeDocument>(configuration);
        var kinds = ComponentKind.All.Select(kind => kind.Value).ToList();

        // A branch per document shape, each taking the workflows written as it: the workflow is
        // the discriminator, so only the branch a component's workflow selects reports.
        var shapes = WorkflowName.All
            .GroupBy(ComponentDocuments.For)
            .Select(shape => new JsonSchemaBuilder()
                .If(new JsonSchemaBuilder()
                    .Required("kind", "workflow")
                    .Properties(
                        ("kind", new JsonSchemaBuilder().Enum(kinds)),
                        ("workflow", new JsonSchemaBuilder().Enum(shape.Select(workflow => workflow.Value)))))
                .Then(new JsonSchemaBuilder()
                    .Id($"{Id}/component/{Shape(shape.Key)}")
                    .FromType(shape.Key, configuration)));

        return new JsonSchemaBuilder()
            .Schema(MetaSchemas.Draft202012Id)
            .Id(Id)
            .Title("Wolfe.Lab declaration")
            .Description("A service, a component or a node of the lab (ROADMAP.md #14). Name this schema in a file's first line to make it one of the lab's.")
            .Type(SchemaValueType.Object)
            .Required("kind")
            .Properties(("kind", new JsonSchemaBuilder()
                .Description("What the document declares: a service, a node, or what a component is used for.")
                .Enum(["service", "node", .. kinds])))
            .AllOf([
                new JsonSchemaBuilder().If(new JsonSchemaBuilder().Properties(("kind", new JsonSchemaBuilder().Const("service")))).Then(service),
                new JsonSchemaBuilder().If(new JsonSchemaBuilder().Properties(("kind", new JsonSchemaBuilder().Const("node")))).Then(node),
                new JsonSchemaBuilder()
                    .If(new JsonSchemaBuilder().Properties(("kind", new JsonSchemaBuilder().Enum(kinds))))
                    .Then(new JsonSchemaBuilder().Required("workflow").Properties(("workflow", new JsonSchemaBuilder().Enum(WorkflowName.All.Select(workflow => workflow.Value))))),
                .. shapes
            ])
            .Build();
    }
}
