using Wolfe.Lab.Build.Workflows.Forgejo.Models;

namespace Wolfe.Lab.Build.Workflows.Forgejo.Steps;

/// <summary>
/// Tells the running Forgejo about the runner. Safe to re-run: registering an existing secret
/// updates the runner in place.
/// </summary>
/// <remarks>
/// <c>-u git</c>, because Forgejo refuses to run as root, which is what a bare exec is. The secret
/// goes in on stdin, never on the command line; op hands it back with no trailing newline, and
/// <c>--secret-stdin</c> counts one (41 is not 40).
/// </remarks>
[Step("register runner", StepKind.Publish)]
internal sealed class RegisterRunner(ICommandRunner commands, ISecretProvider secrets, WorkflowJob job, IWorkflowLog log)
{
    internal const string Container = "forgejo";

    public async Task<StepResult> Run(RunnerRegistration registration, CancellationToken ct = default)
    {
        if (job.DryRun)
        {
            log.Skipped($"Would register {registration.Name} with labels {registration.Labels}, scope {registration.Scope ?? "instance"}.");
            return StepResult.Successful;
        }

        var secret = await secrets.Resolve(registration.Secret.Value, ct);
        string[] scope = registration.Scope is { } repository ? ["--scope", repository] : [];
        await commands.Run(
            Command.Create("docker")
                .WithArguments(
                [
                    "exec", "-i", "-u", "git", Container,
                    "forgejo", "forgejo-cli", "actions", "register",
                    "--secret-stdin", "--name", registration.Name, "--labels", registration.Labels,
                    .. scope
                ])
                .WithInput(secret)
                .ThrowOnError(),
            ct);

        log.Status($"Registered {registration.Name} (labels {registration.Labels}, scope {registration.Scope ?? "instance"}).");
        return StepResult.Successful;
    }
}
