namespace Wolfe.Lab.Build.Workflows.Immich.Models;

/// <summary>
/// The immich-go image the import runs: built from the slice's Dockerfile, never pulled, since
/// the tool ships no image and the lab installs nothing on the node.
/// </summary>
/// <param name="Tag">The image's tag.</param>
/// <param name="Context">The directory holding the Dockerfile.</param>
public sealed record ImmichGoImage(string Tag, IDirectory Context);
