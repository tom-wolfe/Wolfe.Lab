using Wolfe.Lab.Build.Clients.Secrets;

namespace Wolfe.Lab.Build.Workflows.DotNetTool.Models;

/// <summary>
/// Where the key that may publish to the feed is: it stays a reference until the push.
/// </summary>
/// <param name="Reference">The vault reference.</param>
public sealed record FeedToken(SecretReference Reference);
