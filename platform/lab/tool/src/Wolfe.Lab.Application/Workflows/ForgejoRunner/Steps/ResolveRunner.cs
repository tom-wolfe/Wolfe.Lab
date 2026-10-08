using Wolfe.Lab.Application.Workflows.ForgejoRunner.Models;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Forgejo;
using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Application.Workflows.ForgejoRunner.Steps;

/// <summary>
/// Turns the request into the registration the server will be told.
/// </summary>
[Step("resolve runner", StepKind.Work)]
internal sealed class ResolveRunner(RunnerRequest request, IWorkflowLog log)
{
    public StepResult<RunnerRegistration> Run(DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(WorkflowName.ForgejoRunner).TryGetValue(out var declared, out var errors))
        {
            return StepResult.Failed(errors);
        }

        var runners = (ForgejoRunnerComponent)declared;

        var registration = request.Kind switch
        {
            RunnerKind.Host => new RunnerRegistration(request.Node, $"{request.Node}:host", runners.Repository, Reference(runners.Vault, request.Node)),
            RunnerKind.Docker => new RunnerRegistration($"{request.Node}-docker", $"docker:docker://{runners.Image}", null, Reference(runners.Vault, $"{request.Node}-docker")),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Kind, "Not a runner kind.")
        };

        log.Detail($"{registration.Name}: labels {registration.Labels}, scope {registration.Scope ?? "instance"}.");
        return registration;
    }

    private static SecretReference Reference(string vault, string name) => SecretReference.From($"op://{vault}/forgejo-runner-{name}/credential");
}
