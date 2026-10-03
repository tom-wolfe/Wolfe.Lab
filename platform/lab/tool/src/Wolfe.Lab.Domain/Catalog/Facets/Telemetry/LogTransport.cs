namespace Wolfe.Lab.Domain.Catalog.Facets.Telemetry;

/// <summary>
/// How a service's logs reach the lab, when not as the collector reads every container's: its output.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
[Instance("Otlp", "otlp", "The service sends its own over OTLP, so its output is left alone rather than stored twice.")]
public readonly partial struct LogTransport : IClosedSet<LogTransport>
{
    /// <inheritdoc />
    public static IReadOnlyList<LogTransport> All => [Otlp];

    private static Validation Validate(string input) =>
        All.Any(delivery => delivery.Value == input)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not a way logs are delivered ({string.Join(", ", All)}).");
}
