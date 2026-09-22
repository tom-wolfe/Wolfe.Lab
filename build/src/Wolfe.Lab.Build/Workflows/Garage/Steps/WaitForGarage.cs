using Wolfe.Lab.Build.Clients.Garage;

namespace Wolfe.Lab.Build.Workflows.Garage.Steps;

/// <summary>
/// Waits for the daemon to answer: at bring-up the stack has only just started.
/// </summary>
[Step("wait for garage", StepKind.Work)]
internal sealed class WaitForGarage(IGarage garage, IWorkflowLog log, TimeProvider time)
{
    internal const int Attempts = 30;
    internal static readonly TimeSpan Between = TimeSpan.FromSeconds(2);

    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        for (var attempt = 1; attempt <= Attempts; attempt++)
        {
            if (await garage.IsReady(ct))
            {
                log.Detail("Garage is answering.");
                return StepResult.Successful;
            }

            await Task.Delay(Between, time, ct);
        }

        return new Error($"Garage did not answer in {Attempts * Between.TotalSeconds:0}s: is the stack up?");
    }
}
