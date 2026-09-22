using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Workflows.Forgejo.Models;

namespace Wolfe.Lab.Build.Workflows.Forgejo.Steps;

/// <summary>
/// Turns the request into the registration the server will be told.
/// </summary>
[Step("resolve runner", StepKind.Work)]
internal sealed class ResolveRunner(RunnerRequest request, RunnerSettings settings, IWorkflowLog log)
{
    public StepResult<RunnerRegistration> Run()
    {
        if (settings is not { Vault: { Length: > 0 } vault, Repository: { Length: > 0 } repository, Image: { Length: > 0 } image })
        {
            return new Error("'runners.vault', 'runners.repository' and 'runners.image' must all be set in ritten.json.");
        }

        var registration = request.Kind switch
        {
            RunnerKind.Host => new RunnerRegistration(request.Node, $"{request.Node}:host", repository, Reference(vault, request.Node)),
            RunnerKind.Docker => new RunnerRegistration($"{request.Node}-docker", $"docker:docker://{image}", null, Reference(vault, $"{request.Node}-docker")),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Kind, "Not a runner kind.")
        };

        log.Detail($"{registration.Name}: labels {registration.Labels}, scope {registration.Scope ?? "instance"}.");
        return registration;
    }

    private static SecretReference Reference(string vault, string name) => SecretReference.From($"op://{vault}/forgejo-runner-{name}/credential");
}
