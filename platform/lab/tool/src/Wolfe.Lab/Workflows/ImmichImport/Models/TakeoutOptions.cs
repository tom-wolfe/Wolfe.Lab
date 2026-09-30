using Wolfe.Lab.Values;

namespace Wolfe.Lab.Workflows.ImmichImport.Models;

/// <summary>
/// The Google Takeout to import.
/// </summary>
public sealed record TakeoutOptions
{
    /// <summary>
    /// The directory holding the Takeout's zip parts.
    /// </summary>
    public HostPath? Path { get; init; }
}
