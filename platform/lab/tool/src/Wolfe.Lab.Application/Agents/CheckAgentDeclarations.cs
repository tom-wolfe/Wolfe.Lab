using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Catalog;
using Wolfe.Lab.Domain.Catalog.Components.Agents;
using Wolfe.Lab.Domain.Secrets;
using Wolfe.Lab.Infrastructure.Agents;

namespace Wolfe.Lab.Application.Agents;

/// <summary>
/// Judges the component's agent on every node it runs on, as a pull request would want it judged:
/// without being on any of them.
/// </summary>
[Step("check agent declarations", StepKind.Check)]
internal sealed class CheckAgentDeclarations(AgentResolver agents, WorkflowJob job, IWorkflowLog log)
{
    public StepResult Run(ServiceCatalog catalog, DeploymentUnit unit)
    {
        if (!unit.ByWorkflow(job.WorkflowName).TryGetValue(out var component, out var unowned))
        {
            return StepResult.Failed(unowned);
        }

        if (component is not AgentComponent agent)
        {
            return AgentDeclarationErrors.NoAgent(component);
        }

        var errors = new List<Error>();
        var nodes = 0;
        foreach (var node in agent.RunsOn.In(catalog))
        {
            nodes++;
            if (!agents.On(agent, node).TryGetValue(out var declarations, out var invalid))
            {
                errors.AddRange(invalid.Select(error => new Error($"{node}: {error.Message}")));
                continue;
            }

            foreach (var (name, declared) in declarations.Agents)
            {
                errors.AddRange(Judge(node.Name.Value, name, declared));
            }
        }

        if (errors.Count > 0)
        {
            return StepResult.Failed(errors);
        }

        log.Detail($"{component} holds on {nodes} node{(nodes == 1 ? "" : "s")}.");
        return StepResult.Successful;
    }

    private IEnumerable<Error> Judge(string node, string name, AgentOptions agent)
    {
        if (AgentLabel.ForName(name) is null)
        {
            yield return new Error($"{node}: '{name}' cannot name an agent: no dots, slashes or whitespace.");
        }

        if (agent.Program is null)
        {
            yield return new Error($"{node}: agent '{name}' names no 'program'.");
        }

        foreach (var variable in agents.HomePathsIn(agent.Environment))
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
