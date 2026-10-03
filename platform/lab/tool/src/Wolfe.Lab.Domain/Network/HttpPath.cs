namespace Wolfe.Lab.Domain.Network;

/// <summary>
/// The path of an HTTP request, from its root: <c>/metrics</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.TypeConverter)]
public readonly partial struct HttpPath
{
    /// <summary>
    /// Where a Prometheus exporter serves its metrics unless it says otherwise.
    /// </summary>
    public static HttpPath Metrics { get; } = From("/metrics");

    private static Validation Validate(string input) =>
        input.StartsWith('/') && !input.Any(c => char.IsWhiteSpace(c) || c is '?' or '#')
            ? Validation.Ok
            : Validation.Invalid(NetworkErrors.NotAPath(input).Message);
}
