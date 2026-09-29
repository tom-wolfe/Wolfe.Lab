namespace Wolfe.Lab.Build.Clients.Packages;

/// <summary>
/// The tools a job runs, which it makes sure of before its first step that runs one.
/// </summary>
/// <param name="Names">The commands, as the manifest keys them.</param>
public sealed record RequiredTools(IReadOnlyList<string> Names);
