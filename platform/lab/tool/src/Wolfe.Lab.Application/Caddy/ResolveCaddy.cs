using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Infrastructure.Caddy;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Caddy;

/// <summary>
/// The Caddy the component is part of: its container, and the Caddyfile it runs with, which its
/// deployment carries; or the one its <c>ritten.json</c> names while it is part of none.
/// </summary>
[Step("resolve caddy", StepKind.Work)]
internal sealed class ResolveCaddy(DeclaredComponents declared, CaddyOptions legacy, IOptions<LabDirectories> directories, IWorkflowLog log)
{
    private const string Caddyfile = "Caddyfile";

    public async Task<StepResult<CaddyInstance>> Run(ServiceCatalog catalog, CancellationToken ct = default)
    {
        if (await declared.Find<Component>(catalog, ct) is { PartOf: { } whole } component)
        {
            if (component.Service.FindComponent(whole) is not DockerComponent caddy
                || catalog.DeploymentUnitAt(caddy.Directory)?.Value is not { } unit)
            {
                return new Error($"{component.Name} is part of '{whole}', which {component.Service.Name} deploys as no Docker component.");
            }

            var instance = new CaddyInstance(caddy.ComposeService.Value, $"{directories.Value.MountedAt(unit)}/{Caddyfile}");
            log.Detail($"Reloads {caddy.Name}, as {instance.Container}, from {instance.Caddyfile}.");
            return instance;
        }

        return legacy.ToInstance() is { } named ? named : new Error("The component is part of no Caddy.");
    }
}
