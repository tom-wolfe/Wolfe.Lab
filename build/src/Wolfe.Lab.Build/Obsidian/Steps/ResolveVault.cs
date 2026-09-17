using Wolfe.Lab.Build.Obsidian.Models;

namespace Wolfe.Lab.Build.Obsidian.Steps;

/// <summary>
/// Resolves the selected vault.
/// </summary>
[Step("resolve vault", StepKind.Check)]
internal sealed class ResolveVault(KnownVaults vaults, RequestedVault requested)
{
    public StepResult<Vault> Run()
    {
        if (!vaults.ByName.TryGetValue(requested.Name, out var vault))
        {
            return new Error($"No vault named '{requested.Name}'; ritten.json declares {string.Join(", ", vaults.ByName.Keys)}.");
        }

        if (!vault.Directory.Exists)
        {
            return new Error($"{vault.Directory.AbsolutePath} does not exist — see obsidian/RUNBOOK.md.");
        }

        return vault;
    }
}
