using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Application.Workflows.Agents.Models;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Infrastructure.Secrets;

namespace Wolfe.Lab.Application.Workflows.Agents.Steps;

/// <summary>
/// Judges the component's agent on every node it runs on, as a pull request would want it judged:
/// without being on any of them.
/// </summary>
/// <remarks>
/// What a deploy can only find out on the node — whether the program is there — is left to the
/// deploy. What the declarations alone decide is caught here: a placeholder a node has nothing
/// for, a name that cannot be a label, an agent with no program, and a vault reference that
/// would not resolve because it is not one. While <c>ritten.json</c> declares the agents per node,
/// each of its nodes is judged instead.
/// </remarks>
[Step("check agent declarations", StepKind.Check)]
internal sealed class CheckAgentDeclarations(AgentsDeclaredPerNode perNode, WorkflowJob job, IWorkflowLog log)
{
    public StepResult Run(ServiceCatalog catalog, DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(job.WorkflowName).TryGetValue(out var component, out var unowned))
        {
            return StepResult.Failed(unowned);
        }

        var errors = new List<Error>();
        var nodes = 0;
        if (component is AgentComponent agent)
        {
            foreach (var node in agent.RunsOn.In(catalog))
            {
                nodes++;
                if (!agent.RunningOn(node).TryGetValue(out var process, out var unexpanded))
                {
                    errors.AddRange(unexpanded.Select(error => new Error($"{node}: {error.Message}")));
                    continue;
                }

                if (!ResolveAgentDeclarations.Options(process, node, component).TryGetValue(out var resolved, out var invalid))
                {
                    errors.AddRange(invalid.Select(error => new Error($"{node}: {error.Message}")));
                    continue;
                }

                errors.AddRange(Judge(node.Name.Value, process.Name.Value, resolved));
            }
        }
        else
        {
            foreach (var (node, declared) in perNode.Nodes.OrderBy(n => n.Key, StringComparer.Ordinal))
            {
                nodes++;
                if (declared.Agents.Count == 0)
                {
                    errors.Add(new Error($"{node} declares no agents."));
                }

                foreach (var (name, declaredAgent) in declared.Agents)
                {
                    errors.AddRange(Judge(node, name, declaredAgent));
                }
            }

            if (nodes == 0)
            {
                errors.Add(new Error($"{component} declares no agent, and ritten.json no nodes: declare its runsOn, agent and program."));
            }
        }

        if (errors.Count > 0)
        {
            return StepResult.Failed(errors);
        }

        log.Detail($"{component} holds on {nodes} node{(nodes == 1 ? "" : "s")}.");
        return StepResult.Successful;
    }

    private static IEnumerable<Error> Judge(string node, string name, AgentOptions agent)
    {
        if (AgentLabel.ForName(name) is null)
        {
            yield return new Error($"{node}: '{name}' cannot name an agent: no dots, slashes or whitespace.");
        }

        if (agent.Program is null)
        {
            yield return new Error($"{node}: agent '{name}' names no 'program'.");
        }

        foreach (var variable in ResolveAgents.UnexpandedHomePaths(agent.Environment))
        {
            yield return new Error($"{node}: agent '{name}' sets {variable} to a path under ~, which nothing expands: write the absolute path.");
        }

        foreach (var (variable, value) in agent.Environment)
        {
            if (value.StartsWith("op://", StringComparison.Ordinal) && !SecretReference.TryFrom(value, out _))
            {
                yield return new Error($"{node}: agent '{name}' sets {variable} to '{value}', which is not op://<vault>/<item>/<field>.");
            }
        }
    }
}
