using Wolfe.Lab.Build.Http;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Immich.Models;

/// <summary>
/// The Immich server as the import reaches it.
/// </summary>
public sealed record ServerSettings
{
    /// <summary>
    /// The server's URL from inside the lab network, where the import container runs.
    /// </summary>
    public ServiceUrl? Url { get; init; }

    /// <summary>
    /// Where the API key the import authenticates with is.
    /// </summary>
    public SecretReference? ApiKey { get; init; }
}
