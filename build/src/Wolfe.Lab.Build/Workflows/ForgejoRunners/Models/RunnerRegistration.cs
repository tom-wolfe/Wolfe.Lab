using Wolfe.Lab.Build.Clients.Secrets;

namespace Wolfe.Lab.Build.Workflows.ForgejoRunners.Models;

/// <summary>
/// One registration, as the server is told it.
/// </summary>
/// <param name="Name">The runner's name.</param>
/// <param name="Labels">Its labels, as <c>forgejo-cli</c> spells them.</param>
/// <param name="Scope">The repository it is scoped to, or null for the whole instance.</param>
/// <param name="Secret">The vault item holding its secret.</param>
public sealed record RunnerRegistration(string Name, string Labels, string? Scope, SecretReference Secret);
