using Wolfe.Lab.Build.Clients.Ollama;
using Wolfe.Lab.Build.Workflows.Ollama.Models;

namespace Wolfe.Lab.Build.Workflows.Ollama.Steps;

/// <summary>
/// Points each role's name at the model that fills it on this node, and takes away the names
/// of roles the node no longer declares.
/// </summary>
[Step("point roles", StepKind.Publish)]
internal sealed class PointRoles(RolePlan plan, IOllama ollama, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        var identities = await ollama.Identities(ct);

        foreach (var (alias, model) in plan.Roles)
        {
            if (identities.TryGetValue(alias, out var current)
                && identities.TryGetValue(model, out var wanted)
                && current == wanted)
            {
                log.Detail($"{alias.Value} is {model.Value}.");
                continue;
            }

            await ollama.Copy(model, alias, ct);
            if (!job.DryRun)
            {
                log.Status($"Pointed {alias.Value} at {model.Value}.");
            }
        }

        foreach (var stale in identities.Keys.Where(RolePlan.IsAlias).Where(a => !plan.Roles.ContainsKey(a)).OrderBy(a => a.Value, StringComparer.Ordinal))
        {
            await ollama.Remove(stale, ct);
            if (!job.DryRun)
            {
                log.Status($"Removed {stale.Value}: no longer a declared role.");
            }
        }

        return StepResult.Successful;
    }
}
