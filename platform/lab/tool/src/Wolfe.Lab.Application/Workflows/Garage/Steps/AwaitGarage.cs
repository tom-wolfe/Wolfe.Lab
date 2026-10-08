using Wolfe.Lab.Infrastructure.Garage;

namespace Wolfe.Lab.Application.Workflows.Garage.Steps;

/// <summary>
/// Waits for the daemon the converge just started, or restarted, to answer.
/// </summary>
[Step("await garage", StepKind.Work)]
internal sealed class AwaitGarage(IGarage garage, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        if (!await garage.AwaitReady(ct))
        {
            return new Error("Garage did not answer after being converged: see its log.");
        }

        log.Detail("Garage is answering.");
        return StepResult.Successful;
    }
}
