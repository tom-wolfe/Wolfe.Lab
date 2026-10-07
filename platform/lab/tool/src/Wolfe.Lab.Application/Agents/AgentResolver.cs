using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Packages;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Agents;

/// <summary>
/// What the steps that operate agents share: which node this is, what an agents component runs on a
/// node, and a declaration with this node's roots and its package written in.
/// </summary>
internal sealed class AgentResolver(IOptions<LabDirectories> options, IOptions<LabNode> here)
{
    /// <summary>
    /// The agent's installed package, as a catalog declaration writes it: the one placeholder only
    /// the install can expand.
    /// </summary>
    private const string PackagePlaceholder = "{package}";

    /// <summary>
    /// The node this runs on: the one <c>LAB_NODE</c> names, keeping the lab where this run's
    /// environment says it does.
    /// </summary>
    /// <remarks>
    /// chezmoi exports <c>LAB_ROOT</c> and <c>LAB_DATA</c> from the node's declaration, so the two
    /// differ only between a change to it and chezmoi applying that; what this run installs and what
    /// an agent is told must be one place.
    /// </remarks>
    public Result<Node> ThisNode(ServiceCatalog catalog)
    {
        if (here.Value.Name is not { } name || catalog.FindNode(name) is not { } node)
        {
            return AgentDeclarationErrors.NotANode(here.Value.Given);
        }

        var declared = LabDirectories.ForNode(node);
        var environment = options.Value;
        return declared.Root.AbsolutePath == environment.Root.AbsolutePath && declared.Data.AbsolutePath == environment.Data.AbsolutePath
            ? node
            : AgentDeclarationErrors.DirectoriesDiffer(node, declared, environment);
    }

    /// <summary>
    /// What <paramref name="agent"/> runs on <paramref name="node"/>, as the steps that converge it
    /// read it: its templates expanded for the node, its log the node's to place
    /// (<see cref="LabDirectories.AgentLog"/>), and the node's own variables set
    /// (<see cref="NodeVariables"/>).
    /// </summary>
    public Result<AgentDeclarations> On(AgentComponent agent, Node node)
    {
        if (!agent.RunningOn(node).TryGetValue(out var process, out var unexpanded))
        {
            return unexpanded;
        }

        // Expanded for the node, so all a template still holds is {package}, which the install writes in.
        var program = HostPath.TryFrom(process.Program.Value);
        if (!program.IsSuccess)
        {
            return CatalogError.In(agent.Source, new FieldError("program", new Error(program.Error.ErrorMessage)));
        }

        if (process.Environment.Keys.Where(NodeVariables.Names.Contains).ToList() is { Count: > 0 } declared)
        {
            return declared.Select(Error (variable) => CatalogError.In(agent.Source,
                new FieldError($"environment.{variable}", AgentDeclarationErrors.SetByTheNode(variable)))).ToList();
        }

        var directories = LabDirectories.ForNode(node);
        var environment = process.Environment.ToDictionary(variable => variable.Key, variable => variable.Value.Value, StringComparer.Ordinal);
        foreach (var (variable, value) in NodeVariables.ForNode(node, directories))
        {
            environment[variable] = value;
        }

        return new AgentDeclarations(new Dictionary<string, AgentOptions>
        {
            [process.Name.Value] = new()
            {
                Package = process.Package is { } package
                    ? new PackageOptions { Github = package.Github.Value, Version = package.Version.Value, Asset = package.Asset.Value, Checksums = package.Checksums?.Value }
                    : null,
                Program = program.ValueObject,
                Arguments = [.. process.Arguments.Select(argument => argument.Value)],
                Environment = environment,
                Log = HostPath.From(directories.AgentLog(agent.QualifiedName).AbsolutePath),
                Supersedes = process.Supersedes
            }
        });
    }

    /// <summary>
    /// <paramref name="agent"/> with its package's directory written in, when it has one: the one
    /// placeholder its node could not (<see cref="On"/>).
    /// </summary>
    public AgentOptions Expand(AgentOptions agent, IDirectory? package = null)
    {
        return agent with
        {
            Program = Path(agent.Program),
            Arguments = [.. agent.Arguments.Select(Value)],
            Environment = agent.Environment.ToDictionary(variable => variable.Key, variable => Value(variable.Value), StringComparer.Ordinal),
            WorkingDirectory = Path(agent.WorkingDirectory),
            Log = Path(agent.Log)
        };

        HostPath? Path(HostPath? path) => path is { } value ? HostPath.From(Value(value.Value)) : null;

        string Value(string value) => package is null ? value : value.Replace(PackagePlaceholder, package.AbsolutePath, StringComparison.Ordinal);
    }

    /// <summary>
    /// The variables whose value starts with <c>~</c>. A path setting — <c>program</c>, <c>log</c> —
    /// is expanded when it is read; an environment value is handed to the process as written, and
    /// neither the supervisor nor most programs expand it.
    /// </summary>
    public IEnumerable<string> HomePathsIn(IReadOnlyDictionary<string, string> environment) =>
        environment.Where(variable => variable.Value == "~" || variable.Value.StartsWith("~/", StringComparison.Ordinal))
            .Select(variable => variable.Key)
            .Order(StringComparer.Ordinal);
}
