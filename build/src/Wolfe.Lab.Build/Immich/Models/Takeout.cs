using Wolfe.Lab.Build.Http;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Immich.Models;

/// <summary>
/// Where the Takeout is said to be, before anyone has looked.
/// </summary>
/// <param name="Directory">The directory that should hold the parts.</param>
public sealed record TakeoutLocation(IDirectory Directory);

/// <summary>
/// A Google Takeout on the node: the directory its zip parts sit in.
/// </summary>
/// <param name="Directory">The directory holding the parts.</param>
/// <param name="Parts">How many zip parts it holds.</param>
public sealed record Takeout(IDirectory Directory, int Parts);

/// <summary>
/// The server the import uploads to. The key stays a reference until the moment of use.
/// </summary>
/// <param name="Url">The server's URL from the lab network.</param>
/// <param name="ApiKey">Where the API key is.</param>
public sealed record ImmichServer(ServiceUrl Url, SecretReference ApiKey);

/// <summary>
/// How the import runs.
/// </summary>
/// <param name="Concurrency">Parallel uploads.</param>
public sealed record ImportOptions(int Concurrency);

/// <summary>
/// The immich-go image the import runs: built from the slice's Dockerfile, never pulled, since
/// the tool ships no image and the lab installs nothing on the node.
/// </summary>
/// <param name="Tag">The image's tag.</param>
/// <param name="Context">The directory holding the Dockerfile.</param>
public sealed record ImmichGoImage(string Tag, IDirectory Context);
