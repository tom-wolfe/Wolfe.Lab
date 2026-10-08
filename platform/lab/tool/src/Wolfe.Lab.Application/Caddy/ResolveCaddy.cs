using Microsoft.Extensions.Options;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Compose;
using Wolfe.Lab.Infrastructure.Caddy;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Caddy;

/// <summary>
/// The Caddy the component is part of: its container, and the Caddyfile it runs with, which its
/// deployment carries.
/// </summary>
[Step("resolve caddy", StepKind.Work)]
internal sealed class ResolveCaddy(IOptions<LabDirectories> directories, IWorkflowLog log)
{
    internal const string Caddyfile = "Caddyfile";

    public StepResult<CaddyInstance> Run(ServiceCatalog catalog, DeploymentUnit unit)
    {
        var component = unit.Head;
        if (component.PartOf is not { } whole)
        {
            return new Error($"{component.Name} is part of no Caddy: declare `partOf:` the Docker component Caddy runs as.");
        }

        if (component.Service.FindComponent(whole) is not DockerComponent caddy || catalog.DeploymentUnitAt(caddy.Directory)?.Value is not { } deployed)
        {
            return new Error($"{component.Name} is part of '{whole}', which {component.Service.Name} deploys as no Docker component.");
        }

        var instance = new CaddyInstance(caddy.ComposeService.Value, $"{directories.Value.MountedAt(deployed)}/{Caddyfile}");
        log.Detail($"Reloads {caddy.Name}, as {instance.Container}, from {instance.Caddyfile}.");
        return instance;
    }
}
