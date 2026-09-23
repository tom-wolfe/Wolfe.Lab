using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Workflows.ForgejoRunners.Models;

namespace Wolfe.Lab.Build.Workflows.ForgejoRunners.Steps;

/// <summary>
/// Turns the request into the registration the server will be told.
/// </summary>
[Step("resolve runner", StepKind.Work)]
internal sealed class ResolveRunner(RunnerRequest request, RunnerDefaults defaults, IWorkflowLog log)
{
    public StepResult<RunnerRegistration> Run()
    {
        var (vault, repository, image) = defaults;
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
