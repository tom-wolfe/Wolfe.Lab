namespace Wolfe.Lab.Domain.Catalog.Facets.Heartbeats;

/// <summary>
/// A healthchecks.io check, by the slug its URL names it with: <c>lab-restic-offsite</c>.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
public readonly partial struct HeartbeatSlug
{
    private static Validation Validate(string input) => NameRule.Validate(input, "a check's slug");
}
