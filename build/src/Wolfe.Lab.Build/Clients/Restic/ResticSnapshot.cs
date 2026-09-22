namespace Wolfe.Lab.Build.Clients.Restic;

/// <summary>
/// A snapshot as restic lists it.
/// </summary>
/// <param name="Id">The short id.</param>
/// <param name="Time">When it was taken.</param>
/// <param name="Tags">Its tags.</param>
/// <param name="Paths">The paths it holds.</param>
public sealed record ResticSnapshot(string Id, DateTimeOffset Time, IReadOnlyList<string> Tags, IReadOnlyList<string> Paths)
{
    private const string ImagePrefix = "image:";

    /// <summary>
    /// The image the snapshot was taken under, when the backup recorded one.
    /// </summary>
    public string? Image => Tags.FirstOrDefault(t => t.StartsWith(ImagePrefix, StringComparison.Ordinal))?[ImagePrefix.Length..];
}
