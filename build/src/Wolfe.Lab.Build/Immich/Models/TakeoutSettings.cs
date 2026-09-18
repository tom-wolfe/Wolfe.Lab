using Wolfe.Lab.Build.Paths;

namespace Wolfe.Lab.Build.Immich.Models;

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
