using Wolfe.Lab.Domain.Services;
using Wolfe.Lab.Infrastructure.Secrets;

namespace Wolfe.Lab.Application.Workflows.ImmichImport.Models;

/// <summary>
/// The Immich server as the import reaches it.
/// </summary>
public sealed record ServerOptions
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
