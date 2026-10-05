using Wolfe.Lab.Application.Workflows.Agents.Models;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Packages;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Workflows.Agents.Steps;

/// <summary>
/// What this node runs of the component: its agent as the catalog declares it, expanded for the
/// node — or, while its <c>ritten.json</c> still declares its agents per node, this node's entry.
/// </summary>
/// <remarks>
/// The node is where the deploy runs (<see cref="LabNode"/>), never an argument to it. A
/// component placed elsewhere is nothing to do here, and the deploy stops. Its log is the
/// node's to place (<see cref="LabDirectories.AgentLog"/>), and its own variables
/// (<see cref="NodeVariables"/>) the node's to set.
/// </remarks>
[Step("resolve agent declarations", StepKind.Work)]
internal sealed class ResolveAgentDeclarations(
    AgentsDeclaredPerNode perNode,
    NodeRunner runner,
    IOptions<LabDirectories> options,
    IOptions<LabNode> here,
    WorkflowJob job,
    IWorkflowLog log)
{
    public StepResult<AgentDeclarations> Run(ServiceCatalog catalog, DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(job.WorkflowName).TryGetValue(out var component, out var errors))
        {
            return StepResult.Failed(errors);
        }

        if (component is not AgentComponent agent)
        {
            var declared = perNode.Nodes.GetValueOrDefault(runner.Name)?.Agents ?? [];
            log.Detail($"{component} declares its agents per node in ritten.json; {runner.Name} runs {declared.Count}.");
            return new AgentDeclarations(declared);
        }

        if (here.Value.Name is not { } name || catalog.FindNode(name) is not { } node)
        {
            return AgentDeclarationErrors.NotANode(here.Value.Given);
        }

        if (!agent.RunsOn.Includes(node))
        {
            log.Status($"{component} runs on {agent.RunsOn}, which is not {node}: nothing to do here.");
            return StepResult.NothingToDo;
        }

        // Until chezmoi takes LAB_ROOT and LAB_DATA from the node's declaration, the two can differ;
        // what this run installs and what the agent is told must be one place.
        var nodes = LabDirectories.ForNode(node);
        var environment = options.Value;
        if (nodes.Root.AbsolutePath != environment.Root.AbsolutePath || nodes.Data.AbsolutePath != environment.Data.AbsolutePath)
        {
            return AgentDeclarationErrors.DirectoriesDiffer(node, nodes, environment);
        }

        if (!agent.RunningOn(node).TryGetValue(out var process, out var unexpanded))
        {
            return StepResult.Failed(unexpanded);
        }

        if (!Options(process, node, component).TryGetValue(out var resolved, out var invalid))
        {
            return StepResult.Failed(invalid);
        }

        log.Detail($"{component} runs on {node} as {process.Name}.");
        return new AgentDeclarations(new Dictionary<string, AgentOptions> { [process.Name.Value] = resolved });
    }

    /// <summary>
    /// The agent as the steps that converge it read it, on <paramref name="node"/>.
    /// </summary>
    internal static Result<AgentOptions> Options(AgentProcess process, Node node, Component component)
    {
        var directories = LabDirectories.ForNode(node);
        // Expanded for the node, so all a template still holds is {package}, which the install writes in.
        var program = HostPath.TryFrom(process.Program.Value);
        if (!program.IsSuccess)
        {
            return CatalogError.In(component.Source, new FieldError("program", new Error(program.Error.ErrorMessage)));
        }

        if (process.Environment.Keys.Where(NodeVariables.Names.Contains).ToList() is { Count: > 0 } declared)
        {
            return declared.Select(Error (variable) => CatalogError.In(component.Source,
                new FieldError($"environment.{variable}", AgentDeclarationErrors.SetByTheNode(variable)))).ToList();
        }

        var environment = process.Environment.ToDictionary(variable => variable.Key, variable => variable.Value.Value, StringComparer.Ordinal);
        foreach (var (variable, value) in NodeVariables.ForNode(node, directories))
        {
            environment[variable] = value;
        }

        return new AgentOptions
        {
            Package = process.Package is { } package
                ? new PackageOptions { Github = package.Github.Value, Version = package.Version.Value, Asset = package.Asset.Value, Checksums = package.Checksums?.Value }
                : null,
            Program = program.ValueObject,
            Arguments = [.. process.Arguments.Select(argument => argument.Value)],
            Environment = environment,
            Log = HostPath.From(directories.AgentLog(component.QualifiedName).AbsolutePath),
            Supersedes = process.Supersedes
        };
    }
}
