using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// Where an agents component runs, as its file writes it: a rule — <c>all</c>,
/// <c>every server</c> — or the nodes it names, <c>[mini]</c>.
/// </summary>
/// <param name="Rule">The rule, when it is one.</param>
/// <param name="Nodes">The nodes, when it names them.</param>
[JsonConverter(typeof(Converter))]
internal sealed record DeploymentTargetDocument(string? Rule, IReadOnlyList<string>? Nodes)
{
    private sealed class Converter : JsonConverter<DeploymentTargetDocument>
    {
        public override DeploymentTargetDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.String
                ? new DeploymentTargetDocument(reader.GetString(), null)
                : new DeploymentTargetDocument(null, JsonSerializer.Deserialize<List<string>>(ref reader, options));

        public override void Write(Utf8JsonWriter writer, DeploymentTargetDocument value, JsonSerializerOptions options)
        {
            if (value.Rule is { } rule)
            {
                writer.WriteStringValue(rule);
            }
            else
            {
                JsonSerializer.Serialize(writer, value.Nodes ?? [], options);
            }
        }
    }

    /// <summary>
    /// Writes a placement's schema: a rule, or a list of node names.
    /// </summary>
    internal sealed class Schemas : ISchemaGenerator
    {
        public static Schemas Instance { get; } = new();

        public bool Handles(Type type) => type == typeof(DeploymentTargetDocument);

        public void AddConstraints(SchemaGenerationContextBase context) => context.Intents.Add(new AnyOfIntent(
            [new TypeIntent(SchemaValueType.String), new PatternIntent("^(all|every [a-z]+)$")],
            [new TypeIntent(SchemaValueType.Array), new ItemsIntent(SchemaGenerationContextCache.Get(typeof(string))), new MinItemsIntent(1), new UniqueItemsIntent(true)]));
    }
}
