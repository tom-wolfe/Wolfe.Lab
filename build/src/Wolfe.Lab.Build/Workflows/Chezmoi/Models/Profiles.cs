namespace Wolfe.Lab.Build.Workflows.Chezmoi.Models;

/// <summary>
/// The profiles to render, as the step consumes them.
/// </summary>
/// <param name="Names">The profile names, in declaration order.</param>
public sealed record Profiles(IReadOnlyList<string> Names);
