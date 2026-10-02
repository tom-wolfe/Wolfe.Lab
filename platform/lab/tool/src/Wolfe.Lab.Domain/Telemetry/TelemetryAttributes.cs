namespace Wolfe.Lab.Domain.Telemetry;

/// <summary>
/// The rules a list of the lab's attributes is held to wherever a store is told about them.
/// </summary>
public static class TelemetryAttributes
{
    /// <summary>
    /// The attributes <paramref name="listed"/> names, when it names every one of
    /// <paramref name="expected"/> and nothing else — so a new attribute fails until the store is
    /// told about it — or what it leaves out and what it names that the lab does not know.
    /// </summary>
    /// <param name="what">The list, for a message: <c>Loki's otlp_config index labels</c>.</param>
    /// <param name="listed">The names it holds, in OpenTelemetry's spelling.</param>
    /// <param name="expected">The attributes it must hold.</param>
    public static Result<IReadOnlyList<TelemetryAttribute>> Exactly(string what, IEnumerable<string> listed, IReadOnlyList<TelemetryAttribute> expected)
    {
        var names = listed.ToHashSet(StringComparer.Ordinal);
        var errors = new List<Error>();
        errors.AddRange(expected.Where(attribute => !names.Contains(attribute.Name)).Select(missing => new Error($"{what} leaves out {missing.Name}.")));
        foreach (var name in names.Order(StringComparer.Ordinal))
        {
            if (TelemetryAttribute.Named(name).Errors is { } unknown)
            {
                errors.AddRange(unknown.Select(error => new Error($"{what}: {error.Message}")));
            }
        }

        return errors.Count > 0 ? errors : Result.Success(expected);
    }
}
