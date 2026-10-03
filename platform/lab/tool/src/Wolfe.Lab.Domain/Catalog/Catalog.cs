namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// Represents all the services available through the lab.
/// </summary>
/// <param name="Services">Every service, by area and name.</param>
public sealed record Catalog(IReadOnlyList<CatalogService> Services)
{
    /// <summary>
    /// Everything the declarations make that holds, and each problem with the document it is
    /// in — so a check of one document can be held to its own problems, against all the rest.
    /// </summary>
    public static CatalogResult Read(IEnumerable<Declared<Service>> services, IEnumerable<Declared<Component>> components)
    {
        var problems = new List<CatalogError>();
        var byDirectory = new Dictionary<RepositoryPath, Declared<Service>>();
        foreach (var service in services.OrderBy(service => service.Source.File.Value, StringComparer.Ordinal))
        {
            if (service.Source.Directories is not [var area, var directory] || service.Source.File.Parent is not { } at)
            {
                problems.Add(CatalogError.OfService(service.Source, "a service is declared in its own directory, area/service/."));
            }
            else if (AreaName.TryFrom(area) is not { IsSuccess: true })
            {
                problems.Add(CatalogError.OfService(service.Source, $"its area's directory, '{area}', is not a name an area can have: lower case, a letter first, then letters, digits and hyphens."));
            }
            else if (service.Declaration.Name.Value != directory)
            {
                problems.Add(CatalogError.OfService(service.Source, $"the service is named '{service.Declaration.Name}', but its directory is '{directory}'."));
            }
            else if (!byDirectory.TryAdd(at, service))
            {
                problems.Add(CatalogError.OfService(service.Source, $"'{directory}' is declared already, in {byDirectory[at].Source}."));
            }
        }

        foreach (var shared in byDirectory.Values.GroupBy(service => service.Declaration.Name).Where(group => group.Count() > 1))
        {
            foreach (var service in shared)
            {
                problems.Add(CatalogError.OfService(service.Source, $"another service is named '{shared.Key}' too ({string.Join(", ", shared.Where(other => other != service).Select(other => other.Source))}); a service's name is the lab's, not its area's."));
            }
        }

        var members = byDirectory.ToDictionary(pair => pair.Key, _ => new List<CatalogComponent>());
        foreach (var component in components.OrderBy(component => component.Source.ToString(), StringComparer.Ordinal))
        {
            if (Place(component, problems) is not { } place)
            {
                continue;
            }

            if (!members.TryGetValue(place.Service, out var siblings))
            {
                problems.Add(CatalogError.OfComponent(component.Source, $"a component belongs to a service, and {place.Service}/ declares none that holds."));
                continue;
            }

            if (siblings.FirstOrDefault(sibling => sibling.Name == place.Name) is { } taken)
            {
                problems.Add(CatalogError.OfComponent(component.Source, $"'{place.Name}' is declared already, in {taken.Source}."));
                continue;
            }

            siblings.Add(new CatalogComponent(place.Name, component.Declaration, component.Source, place.Directory));
        }

        var names = byDirectory.Values.Select(service => service.Declaration.Name).ToHashSet();
        foreach (var service in byDirectory.Values)
        {
            foreach (var needed in service.Declaration.DependsOn.Where(needed => !names.Contains(needed)))
            {
                problems.Add(CatalogError.OfService(service.Source, $"depends on the service '{needed}', which the lab does not declare."));
            }
        }

        foreach (var siblings in members.Values)
        {
            var named = siblings.Select(sibling => sibling.Name).ToHashSet();
            foreach (var component in siblings)
            {
                foreach (var needed in component.Declaration.DependsOn)
                {
                    if (needed == component.Name)
                    {
                        problems.Add(CatalogError.OfComponent(component.Source, $"'{component.Name}' depends on itself."));
                    }
                    else if (!named.Contains(needed))
                    {
                        problems.Add(CatalogError.OfComponent(component.Source, $"depends on '{needed}', which its service does not declare; a component depends only on components of its own service."));
                    }
                }
            }
        }

        var catalog = new Catalog([
            .. byDirectory.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
                .Select(pair => new CatalogService(AreaName.From(pair.Key.Segments[0]), pair.Value.Declaration, pair.Value.Source,
                    [.. members[pair.Key].OrderBy(component => component.Name.Value, StringComparer.Ordinal)]))
        ]);
        return CatalogResult.Of(catalog, problems);
    }

    /// <summary>
    /// The component whose own directory is <paramref name="directory"/>, if one is declared there.
    /// </summary>
    public CatalogComponent? ComponentAt(RepositoryPath directory) =>
        Services.SelectMany(service => service.Components).FirstOrDefault(component => component.Directory == directory);

    /// <summary>
    /// Where a component sits and what it is called: in a directory of its own, named for it; or
    /// beside its service's declaration, named outright.
    /// </summary>
    /// <remarks>
    /// A directory's name is taken as it is: one that is not already a name — not merely one that
    /// would spell as one — is a directory to rename.
    /// </remarks>
    private static (RepositoryPath Service, ComponentName Name, RepositoryPath? Directory)? Place(Declared<Component> component, List<CatalogError> problems)
    {
        var file = component.Source.File;
        switch (file.Directories)
        {
            case [_, _, var directory] when file.Parent is { } own && own.Parent is { } service:
                if (ComponentName.TryFrom(directory) is not { IsSuccess: true } named)
                {
                    problems.Add(CatalogError.OfComponent(component.Source, $"its directory, '{directory}', is not a name a component can have: lower case, a letter first, then letters, digits and hyphens."));
                    return null;
                }

                if (component.Declaration.Name is { } declared && declared != named.ValueObject)
                {
                    problems.Add(CatalogError.OfComponent(component.Source, $"the component is named '{declared}', but its directory is '{directory}'."));
                    return null;
                }

                return (service, named.ValueObject, own);
            case [_, _] when file.Parent is { } service:
                if (component.Declaration.Name is not { } name)
                {
                    problems.Add(CatalogError.OfComponent(component.Source, "a component beside its service's declaration has no directory to be named for, so it needs a 'name'."));
                    return null;
                }

                return (service, name, null);
            default:
                problems.Add(CatalogError.OfComponent(component.Source, "a component is declared in its service's directory, or in a directory of its own within it."));
                return null;
        }
    }
}
