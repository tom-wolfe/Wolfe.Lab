using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Infrastructure.Ollama;

namespace Wolfe.Lab.Application.Workflows.Ollama.Steps;

/// <summary>
/// Makes each use's name the model this server runs for it, with its context — a manifest that
/// shares the model's weights — and retires the name of a use no longer declared, so a caller
/// asking for it hears "not found" rather than whatever it used to mean.
/// </summary>
[Step("point uses", StepKind.Publish)]
internal sealed class PointUses(IOllama ollama, WorkflowJob job, IWorkflowLog log)
{
    public async Task<StepResult> Run(ServerPlan plan, CancellationToken ct = default)
    {
        if (plan.Server is null)
        {
            return StepResult.Successful;
        }

        foreach (var served in plan.Served)
        {
            if (await IsAlready(served, ct))
            {
                log.Detail($"{served.Alias.Value} is {Describe(served)}.");
                continue;
            }

            await ollama.Create(served.Alias, served.Model, served.Context, ct);
            if (!job.DryRun)
            {
                log.Status($"Made {served.Alias.Value} {Describe(served)}.");
            }
        }

        foreach (var stale in (await ollama.Installed(ct)).Where(ServerPlan.IsUse).Where(name => !plan.Uses.Contains(name)).OrderBy(name => name.Value, StringComparer.Ordinal))
        {
            await ollama.Remove(stale, ct);
            if (!job.DryRun)
            {
                log.Status($"Removed {stale.Value}: no longer a declared use.");
            }
        }

        return StepResult.Successful;
    }

    // Built from the same weights, with the same context.
    private async Task<bool> IsAlready(ServedModel served, CancellationToken ct) =>
        await ollama.Describe(served.Alias, ct) is { } current
        && await ollama.Describe(served.Model, ct) is { } wanted
        && current.Weights.SequenceEqual(wanted.Weights)
        && current.Context == (served.Context ?? wanted.Context);

    private static string Describe(ServedModel served) =>
        served.Context is { } context ? $"{served.Model.Value}, with a context of {context}" : served.Model.Value;
}
