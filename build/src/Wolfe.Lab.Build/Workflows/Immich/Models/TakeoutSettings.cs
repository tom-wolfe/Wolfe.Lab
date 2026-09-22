using Wolfe.Lab.Build.Values;

namespace Wolfe.Lab.Build.Workflows.Immich.Models;

/// <summary>
/// The Google Takeout to import.
/// </summary>
public sealed record TakeoutSettings
{
    /// <summary>
    /// The directory holding the Takeout's zip parts.
    /// </summary>
    public HostPath? Path { get; init; }
}
