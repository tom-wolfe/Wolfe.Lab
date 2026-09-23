namespace Wolfe.Lab.Build.Workflows.CaddyRoutes.Models;

/// <summary>
/// Every component's route snippet, gathered into one directory ready to install.
/// </summary>
/// <param name="Directory">The staging directory.</param>
/// <param name="Names">The snippets in it, by the name each is filed under.</param>
public sealed record StagedRoutes(IDirectory Directory, IReadOnlyList<string> Names);
