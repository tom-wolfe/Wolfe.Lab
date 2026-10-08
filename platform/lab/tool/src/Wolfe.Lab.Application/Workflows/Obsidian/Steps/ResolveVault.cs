using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.Obsidian.Models;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Obsidian;
using Wolfe.Lab.Domain.Git;

namespace Wolfe.Lab.Application.Workflows.Obsidian.Steps;

/// <summary>
/// The vault the component is, or the one <c>--vault</c> names of its <c>ritten.json</c>'s while it
/// declares none.
/// </summary>
[Step("resolve vault", StepKind.Check)]
internal sealed class ResolveVault(DeclaredComponents declared, ObsidianOptions legacy, RequestedVault requested)
{
    public async Task<StepResult<Vault>> Run(ServiceCatalog catalog, CancellationToken ct = default)
    {
        Vault vault;
        if (await declared.Find<ObsidianComponent>(catalog, ct) is { } component)
        {
            vault = new Vault(component.Name.Value, component.Path.Directory, component.Repository, component.Push, component.Exclude);
        }
        else if (!legacy.Vaults.TryGetValue(requested.Name, out var written) || written is not { Path: { } path, Repository: { } repository })
        {
            return new Error($"No vault named '{requested.Name}'; ritten.json declares {string.Join(", ", legacy.Vaults.Keys)}.");
        }
        else if (legacy.Push is not { Username: { } username, Token: { } token })
        {
            return new Error("ritten.json names no 'push.username' and 'push.token'.");
        }
        else
        {
            vault = new Vault(requested.Name, path.Directory, repository, new PushCredential(username, token), legacy.Exclude);
        }

        if (!vault.Directory.Exists)
        {
            return new Error($"{vault.Directory.AbsolutePath} does not exist — see personal/obsidian/RUNBOOK.md.");
        }

        return vault;
    }
}
