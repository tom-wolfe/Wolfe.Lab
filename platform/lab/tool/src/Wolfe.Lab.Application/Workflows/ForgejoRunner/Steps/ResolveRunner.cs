using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.ForgejoRunner.Models;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Forgejo;
using Wolfe.Lab.Domain.Secrets;

namespace Wolfe.Lab.Application.Workflows.ForgejoRunner.Steps;

/// <summary>
/// Turns the request into the registration the server will be told.
/// </summary>
[Step("resolve runner", StepKind.Work)]
internal sealed class ResolveRunner(DeclaredComponents declared, ForgejoRunnerOptions legacy, RunnerRequest request, IWorkflowLog log)
{
    public async Task<StepResult<RunnerRegistration>> Run(ServiceCatalog catalog, CancellationToken ct = default)
    {
        var defaults = await declared.Find<ForgejoRunnerComponent>(catalog, ct) is { } runners
            ? new RunnerDefaults(runners.Vault, runners.Repository, runners.Image)
            : legacy.ToDefaults();
        if (defaults is not var (vault, repository, image))
        {
            return new Error("The component declares no vault, repository and image.");
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
