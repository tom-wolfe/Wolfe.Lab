using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Infrastructure.Ollama;

namespace Wolfe.Lab.Application.Workflows.Ollama.Steps;

/// <summary>
/// Works out which declared models the node is missing.
/// </summary>
[Step("resolve models", StepKind.Work)]
internal sealed class ResolveModels(IOllama ollama, IWorkflowLog log)
{
    public async Task<StepResult<ModelPlan>> Run(ServerPlan plan, CancellationToken ct = default)
    {
        if (plan.Server is null)
        {
            return new ModelPlan([]);
        }

        var installed = await ollama.Installed(ct);
        var missing = plan.Models.Where(model => !installed.Contains(model)).ToList();

        // Present but undeclared: reported, never removed. A model is several gigabytes that
        // somebody pulled on purpose, and a config file is a poor reason to delete one.
        // A use's name is a name, not a model, and is the next step's business.
        var extra = installed.Where(model => !plan.Models.Contains(model) && !ServerPlan.IsUse(model)).ToList();
        if (extra.Count > 0)
        {
            log.Detail($"Also on the node, undeclared: {string.Join(", ", extra.Select(m => m.Value))}.");
        }

        log.Detail(missing.Count == 0
            ? $"All {plan.Models.Count} declared model{(plan.Models.Count == 1 ? " is" : "s are")} already here."
            : $"Missing {missing.Count} of {plan.Models.Count} declared.");

        return new ModelPlan(missing);
    }
}
