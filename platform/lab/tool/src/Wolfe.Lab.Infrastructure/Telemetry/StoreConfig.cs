using Wolfe.Lab.Domain.Telemetry;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Wolfe.Lab.Infrastructure.Telemetry;

/// <summary>
/// The parts of a telemetry store's configuration that name the lab's attributes: what
/// Prometheus promotes from OTLP, what Loki indexes, and how Grafana links a span to its logs.
/// </summary>
/// <remarks>
/// One shape for every store's file, each section present only in its own store's: a file is
/// Loki's because it has a <c>limits_config</c>, not because of its name. Grafana spells its keys
/// in camel case and the stores in snake case, so each is named as its file writes it.
/// </remarks>
public sealed class StoreConfig
{
    /// <summary>
    /// The one attribute Loki indexes on OTLP logs without being told to.
    /// </summary>
    public static TelemetryAttribute IndexedByLoki => TelemetryAttribute.ServiceName;

    private static readonly IDeserializer Reader = new DeserializerBuilder().IgnoreUnmatchedProperties().Build();

    /// <summary>
    /// Prometheus's OTLP receiver.
    /// </summary>
    [YamlMember(Alias = "otlp")]
    public PrometheusOtlp? Otlp { get; init; }

    /// <summary>
    /// Loki's limits, where its OTLP index labels are.
    /// </summary>
    [YamlMember(Alias = "limits_config")]
    public LokiLimits? Limits { get; init; }

    /// <summary>
    /// Grafana's provisioned datasources.
    /// </summary>
    [YamlMember(Alias = "datasources")]
    public List<GrafanaDatasource>? Datasources { get; init; }

    /// <summary>
    /// The store's configuration, when each part of it that names the lab's attributes names them
    /// as the lab does. A file that is not YAML, or not a mapping, has no such parts: whether it is
    /// valid is its own tool's to say.
    /// </summary>
    public static Result<StoreConfig> Read(string yaml)
    {
        StoreConfig config;
        try
        {
            config = Reader.Deserialize<StoreConfig?>(yaml) ?? new StoreConfig();
        }
        catch (YamlException)
        {
            config = new StoreConfig();
        }

        var errors = new List<Error>();
        if (config.Otlp?.PromoteResourceAttributes is { } promoted
            && TelemetryAttributes.Exactly("Prometheus's otlp.promote_resource_attributes", promoted, TelemetryAttribute.All).Errors is { } prometheus)
        {
            errors.AddRange(prometheus);
        }

        if (config.Limits is { } limits)
        {
            var indexed = (limits.Otlp?.ResourceAttributes?.AttributesConfig ?? [])
                .Where(entry => entry.Action == "index_label")
                .SelectMany(entry => entry.Attributes ?? []);
            var expected = TelemetryAttribute.All.Where(attribute => attribute != IndexedByLoki).ToList();
            if (TelemetryAttributes.Exactly("Loki's otlp_config index labels", indexed, expected).Errors is { } loki)
            {
                errors.AddRange(loki);
            }
        }

        foreach (var source in config.Datasources ?? [])
        {
            foreach (var tag in source.JsonData?.TracesToLogs?.Tags ?? [])
            {
                if (TelemetryAttribute.Named(tag.Key ?? "") is { Errors: { } unknown })
                {
                    errors.AddRange(unknown.Select(error => new Error($"datasource '{source.Name}' links spans to logs by {tag.Key}: {error.Message}")));
                }
                else if (TelemetryAttribute.Named(tag.Key ?? "").Value is { } attribute && attribute.Label != tag.Value)
                {
                    errors.Add(new Error($"datasource '{source.Name}' links {tag.Key} to the label '{tag.Value}': Loki spells it {attribute.Label}."));
                }
            }
        }

        return errors.Count > 0 ? errors : config;
    }

    /// <summary>
    /// <c>otlp</c> in Prometheus's configuration.
    /// </summary>
    public sealed class PrometheusOtlp
    {
        /// <summary>
        /// The resource attributes promoted to labels.
        /// </summary>
        [YamlMember(Alias = "promote_resource_attributes")]
        public List<string>? PromoteResourceAttributes { get; init; }
    }

    /// <summary>
    /// <c>limits_config</c> in Loki's configuration.
    /// </summary>
    public sealed class LokiLimits
    {
        /// <summary>
        /// How OTLP logs are labelled.
        /// </summary>
        [YamlMember(Alias = "otlp_config")]
        public LokiOtlp? Otlp { get; init; }
    }

    /// <summary>
    /// <c>otlp_config</c> in Loki's limits.
    /// </summary>
    public sealed class LokiOtlp
    {
        /// <summary>
        /// What is done with each resource attribute.
        /// </summary>
        [YamlMember(Alias = "resource_attributes")]
        public LokiResourceAttributes? ResourceAttributes { get; init; }
    }

    /// <summary>
    /// <c>resource_attributes</c> in Loki's OTLP config.
    /// </summary>
    public sealed class LokiResourceAttributes
    {
        /// <summary>
        /// One action per group of attributes.
        /// </summary>
        [YamlMember(Alias = "attributes_config")]
        public List<LokiAttributeAction>? AttributesConfig { get; init; }
    }

    /// <summary>
    /// One entry of Loki's <c>attributes_config</c>.
    /// </summary>
    public sealed class LokiAttributeAction
    {
        /// <summary>
        /// <c>index_label</c>, <c>structured_metadata</c> or <c>drop</c>.
        /// </summary>
        [YamlMember(Alias = "action")]
        public string? Action { get; init; }

        /// <summary>
        /// The attributes it applies to, in OpenTelemetry's spelling.
        /// </summary>
        [YamlMember(Alias = "attributes")]
        public List<string>? Attributes { get; init; }
    }

    /// <summary>
    /// One provisioned datasource.
    /// </summary>
    public sealed class GrafanaDatasource
    {
        /// <summary>
        /// Its name in Grafana.
        /// </summary>
        [YamlMember(Alias = "name")]
        public string? Name { get; init; }

        /// <summary>
        /// Its type-specific settings.
        /// </summary>
        [YamlMember(Alias = "jsonData")]
        public GrafanaJsonData? JsonData { get; init; }
    }

    /// <summary>
    /// A datasource's <c>jsonData</c>.
    /// </summary>
    public sealed class GrafanaJsonData
    {
        /// <summary>
        /// Tempo's link from a span to its logs.
        /// </summary>
        [YamlMember(Alias = "tracesToLogsV2")]
        public GrafanaTracesToLogs? TracesToLogs { get; init; }
    }

    /// <summary>
    /// <c>tracesToLogsV2</c> on a Tempo datasource.
    /// </summary>
    public sealed class GrafanaTracesToLogs
    {
        /// <summary>
        /// Each span attribute and the log label it is matched to.
        /// </summary>
        [YamlMember(Alias = "tags")]
        public List<GrafanaTag>? Tags { get; init; }
    }

    /// <summary>
    /// One pairing of a span attribute with a log label.
    /// </summary>
    public sealed class GrafanaTag
    {
        /// <summary>
        /// The span's attribute, in OpenTelemetry's spelling.
        /// </summary>
        [YamlMember(Alias = "key")]
        public string? Key { get; init; }

        /// <summary>
        /// The log label it is matched to, in Loki's.
        /// </summary>
        [YamlMember(Alias = "value")]
        public string? Value { get; init; }
    }
}
