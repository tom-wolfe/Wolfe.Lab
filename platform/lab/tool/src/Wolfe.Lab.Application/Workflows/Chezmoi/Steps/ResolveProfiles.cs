using Wolfe.Lab.Application.Catalog;
using Wolfe.Lab.Application.Workflows.Chezmoi.Models;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Chezmoi;

namespace Wolfe.Lab.Application.Workflows.Chezmoi.Steps;

/// <summary>
/// The profiles the component declares, or its <c>ritten.json</c> names while it declares none.
/// </summary>
[Step("resolve profiles", StepKind.Work)]
internal sealed class ResolveProfiles(DeclaredComponents declared, ChezmoiOptions legacy, IWorkflowLog log)
{
    public async Task<StepResult<Profiles>> Run(ServiceCatalog catalog, CancellationToken ct = default)
    {
        var profiles = await declared.Find<ChezmoiComponent>(catalog, ct) is { } component
            ? component.Profiles
            : legacy.Profiles.Select(ChezmoiProfile.From).ToList();
        if (profiles.Count == 0)
        {
            return new Error("The component declares no profiles.");
        }

        log.Detail($"Renders {string.Join(", ", profiles.Select(profile => profile.Value))}.");
        return new Profiles(profiles);
    }
}
