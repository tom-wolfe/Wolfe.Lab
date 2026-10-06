using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;

namespace Wolfe.Lab.Infrastructure.Declarations;

/// <summary>
/// Where a component serves its metrics, as its file writes it: one endpoint, or a list of them
/// when its container serves more than one.
/// </summary>
/// <param name="Endpoints">Each endpoint, in the order written.</param>
[JsonConverter(typeof(Converter))]
internal sealed record MetricsDocuments(IReadOnlyList<MetricsDocument> Endpoints)
{
    private sealed class Converter : JsonConverter<MetricsDocuments>
    {
        public override MetricsDocuments Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.StartArray
                ? new MetricsDocuments(JsonSerializer.Deserialize<List<MetricsDocument>>(ref reader, options) ?? [])
                : new MetricsDocuments(JsonSerializer.Deserialize<MetricsDocument>(ref reader, options) is { } one ? [one] : []);

        public override void Write(Utf8JsonWriter writer, MetricsDocuments value, JsonSerializerOptions options)
        {
            if (value.Endpoints is [var one])
            {
                JsonSerializer.Serialize(writer, one, options);
            }
            else
            {
                JsonSerializer.Serialize(writer, value.Endpoints, options);
            }
        }
    }

    /// <summary>
    /// Writes the facet's schema: an endpoint, or a list of at least one.
    /// </summary>
    internal sealed class Schemas : ISchemaGenerator
    {
        public static Schemas Instance { get; } = new();

        public bool Handles(Type type) => type == typeof(MetricsDocuments);

        public void AddConstraints(SchemaGenerationContextBase context)
        {
            var endpoint = SchemaGenerationContextCache.Get(typeof(MetricsDocument));
            context.Intents.Add(new AnyOfIntent(
                endpoint.Intents,
                [new TypeIntent(SchemaValueType.Array), new ItemsIntent(endpoint), new MinItemsIntent(1)]));
        }
    }
}
