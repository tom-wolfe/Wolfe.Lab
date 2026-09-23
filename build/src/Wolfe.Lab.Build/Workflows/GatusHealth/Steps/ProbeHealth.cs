using Wolfe.Lab.Build.Clients.Gatus;
using Wolfe.Lab.Build.Workflows.GatusHealth.Models;

namespace Wolfe.Lab.Build.Workflows.GatusHealth.Steps;

/// <summary>
/// Asks Gatus whether it is up, and fails when the answer is anything else.
/// </summary>
[Step("probe health", StepKind.Check)]
internal sealed class ProbeHealth(IGatus gatus, HealthProbe probe, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        var health = await gatus.Health(probe.Url, ct);
        if (!health.IsUp)
        {
            return new Error($"Gatus reports '{health.Status}' at {probe.Url.Value}.");
        }

        log.Status($"Gatus is up at {probe.Url.Value}.");
        return StepResult.Successful;
    }
}
