using Wolfe.Lab.Clients.Garage;

namespace Wolfe.Lab.Workflows.GarageLayout.Steps;

/// <summary>
/// Waits for the daemon to answer: at bring-up the stack has only just started.
/// </summary>
[Step("wait for garage", StepKind.Work)]
internal sealed class WaitForGarage(IGarage garage, IWorkflowLog log)
{
    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        if (!await garage.AwaitReady(ct))
        {
            return new Error("Garage did not answer: is the stack up?");
        }

        log.Detail("Garage is answering.");
        return StepResult.Successful;
    }
}
