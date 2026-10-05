using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Catalog.Services;

namespace Wolfe.Lab.Domain.Catalog.Components.Agents;

/// <summary>
/// A component that is a host process, run by launchd or systemd.
/// </summary>
public sealed class AgentComponent : Component
{
    // What every value may hold; the package's own fields hold only what names a release asset.
    private static readonly string[] Lab = ["lab.root", "lab.data", "state", "package"];
    private static readonly string[] Platform = ["platform", "platform.os", "platform.arch"];
    private static readonly string[] OfTheNode = ["node.name", "node.role", "node.platform", "node.address", "node.docker", "node.drives"];
    private static readonly string[] Package = [.. Platform, "version"];
    private static readonly string[] Expanded = [.. Lab, .. Platform, .. OfTheNode];

    // What only the install knows: where the package went, and the version its names are written for.
    private static readonly string[] Deferred = ["package", "version"];

    private AgentComponent() { }

    /// <summary>
    /// The nodes it runs on.
    /// </summary>
    public required DeploymentTarget RunsOn { get; init; }

    /// <summary>
    /// The process it runs on each of them.
    /// </summary>
    public required AgentProcess Agent { get; init; }

    /// <summary>
    /// Creates a new agent component.
    /// </summary>
    public static Result<AgentComponent> Create(DocumentSource source, ComponentName name, ComponentKind kind, WorkflowName workflow, ComponentName? partOf,
        IReadOnlyList<ComponentName> dependsOn, DeploymentTarget runsOn, AgentProcess agent)
    {
        var errors = Validate(source, name, partOf, dependsOn, out var directory);
        errors.AddRange(Problems(agent).Select(Error (problem) => CatalogError.In(source, problem)));
        if (errors.Count != 0)
        {
            return errors;
        }

        return new AgentComponent
        {
            Source = source,
            Directory = directory,
            Name = name,
            Kind = kind,
            Workflow = workflow,
            PartOf = partOf,
            DependsOn = dependsOn,
            RunsOn = runsOn,
            Agent = agent
        };
    }

    private static IEnumerable<Error> Problems(AgentProcess agent)
    {
        foreach (var (field, value) in agent.Values)
        {
            foreach (var placeholder in value.Placeholders.Where(placeholder => !Expanded.Contains(placeholder) && AddressOf(placeholder) is null))
            {
                yield return new FieldError(field, AgentErrors.UnknownPlaceholder(placeholder, Expanded));
            }
        }

        foreach (var (field, value) in agent.PackageValues)
        {
            foreach (var placeholder in value.Placeholders.Where(placeholder => !Package.Contains(placeholder)))
            {
                yield return new FieldError(field, AgentErrors.UnknownPlaceholder(placeholder, Package));
            }
        }
    }

    // The node a {node.<name>.address} names, or null for any other placeholder.
    private static NodeName? AddressOf(string placeholder) =>
        placeholder.Split('.') is ["node", var node, "address"] && NodeName.TryFrom(node) is { IsSuccess: true } name ? name.ValueObject : null;

    /// <inheritdoc />
    /// <remarks>
    /// The nodes it runs on, and any node a template names, must be the catalog's.
    /// </remarks>
    internal override IReadOnlyList<Error> SetService(Service service)
    {
        var catalog = service.Catalog;
        var problems = new List<Error>();
        problems.AddRange(RunsOn.Named.Where(node => catalog.FindNode(node) is null).Select(Error (node) => new FieldError("runsOn", NodeErrors.Undeclared(node))));
        if (RunsOn.Named.Count == 0 && RunsOn.In(catalog).Count == 0)
        {
            problems.Add(new FieldError("runsOn", NodeErrors.EmptyTarget(RunsOn)));
        }

        foreach (var (field, value) in Agent.Values)
        {
            problems.AddRange(value.Placeholders.Select(AddressOf).OfType<NodeName>().Where(node => catalog.FindNode(node) is null)
                .Select(Error (node) => new FieldError(field, NodeErrors.Undeclared(node))));
        }

        return problems.Count > 0 ? problems : base.SetService(service);
    }

    /// <summary>
    /// Its process as it runs on <paramref name="node"/>.
    /// </summary>
    public Result<AgentProcess> RunningOn(Node node)
    {
        var catalog = Service.Catalog;
        var missing = new List<Error>();
        var expanded = Agent with
        {
            Program = Expand("program", Agent.Program),
            Arguments = [.. Agent.Arguments.Select((argument, index) => Expand($"arguments.{index}", argument))],
            Environment = Agent.Environment.ToDictionary(variable => variable.Key, variable => Expand($"environment.{variable.Key}", variable.Value), StringComparer.Ordinal),
            Package = Agent.Package is { } package
                ? package with
                {
                    Asset = Expand("package.asset", package.Asset),
                    Checksums = package.Checksums is { } checksums ? Expand("package.checksums", checksums) : null
                }
                : null
        };
        return missing.Count > 0 ? missing.Select(Error (problem) => CatalogError.In(Source, problem)).ToList() : expanded;

        // What the node has no value for is left in the template; only the install's are left on purpose.
        Template Expand(string field, Template value)
        {
            var written = value.Expand(Value);
            missing.AddRange(written.Placeholders.Where(placeholder => !Deferred.Contains(placeholder))
                .Select(Error (placeholder) => new FieldError(field, AgentErrors.NothingFor(placeholder, node))));
            return written;
        }

        string? Value(string placeholder) => placeholder switch
        {
            "lab.root" => node.Directories.Root.Value,
            "lab.data" => node.Directories.Data.Value,
            "state" => $"{node.Directories.Data}/{Service.Name}",
            "platform" => node.Platform.Value,
            "platform.os" => node.Platform.Os,
            "platform.arch" => node.Platform.Arch,
            "node.name" => node.Name.Value,
            "node.role" => node.Role.Value,
            "node.platform" => node.Platform.Value,
            "node.address" => node.Address.Value,
            "node.docker" => node.Docker?.Value,
            "node.drives" => string.Join(',', node.Drives),
            _ when AddressOf(placeholder) is { } other => catalog.FindNode(other)?.Address.Value,
            _ => null
        };
    }
}
