using Wolfe.Lab.Domain.Services;
using Wolfe.Lab.Infrastructure.Secrets;

namespace Wolfe.Lab.Application.Workflows.ImmichImport.Models;

/// <summary>
/// The server the import uploads to. The key stays a reference until the moment of use.
/// </summary>
/// <param name="Url">The server's URL from the lab network.</param>
/// <param name="ApiKey">Where the API key is.</param>
public sealed record ImmichServer(ServiceUrl Url, SecretReference ApiKey);
