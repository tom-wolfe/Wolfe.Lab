namespace Wolfe.Lab.Domain.Catalog.Services;

/// <summary>
/// What a link from a service's catalog entry points at.
/// </summary>
[ValueObject<string>(conversions: Conversions.SystemTextJson | Conversions.TypeConverter)]
[Instance("App", "app", "The service itself, where people use it.")]
[Instance("Runbook", "runbook", "Its runbook.")]
[Instance("Docs", "docs", "Its documentation, the lab's or upstream's.")]
[Instance("Dashboard", "dashboard", "A dashboard of it.")]
[Instance("Repository", "repository", "Its source.")]
public readonly partial struct LinkType : IClosedSet<LinkType>
{
    /// <inheritdoc />
    public static IReadOnlyList<LinkType> All => [App, Runbook, Docs, Dashboard, Repository];

    private static Validation Validate(string input) =>
        All.Any(type => type.Value == input)
            ? Validation.Ok
            : Validation.Invalid($"'{input}' is not a type of link ({string.Join(", ", All)}).");
}
