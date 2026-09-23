namespace Wolfe.Lab.Build.Clients.Releases;

/// <summary>
/// The name a component's <c>ritten.json</c> installs it under.
/// </summary>
/// <param name="Value">The directory name under the release root.</param>
public sealed record ReleaseName(string Value);
