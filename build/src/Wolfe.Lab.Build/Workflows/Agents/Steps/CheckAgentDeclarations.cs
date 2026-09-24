using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Clients.Secrets;
using Wolfe.Lab.Build.Workflows.Agents.Models;

namespace Wolfe.Lab.Build.Workflows.Agents.Steps;

/// <summary>
/// Judges every node's declarations as a pull request would want them judged: without a node.
/// </summary>
/// <remarks>
/// What a deploy can only find out on the node — whether the program is there — is left to the
/// deploy. What the file alone decides is caught here: a name that cannot be a label, an agent
/// with no program, and a vault reference that would not resolve because it is not one.
/// </remarks>
[Step("check agent declarations", StepKind.Check)]
internal sealed class CheckAgentDeclarations(AgentsDeclaredPerNode nodes, IWorkflowLog log)
{
    public StepResult Run()
    {
        var errors = new List<Error>();
        foreach (var (node, declared) in nodes.Nodes.OrderBy(n => n.Key, StringComparer.Ordinal))
        {
            if (declared.Agents.Count == 0)
            {
                errors.Add(new Error($"{node} declares no agents."));
            }

            foreach (var (name, agent) in declared.Agents)
            {
                if (AgentLabel.ForName(name) is null)
                {
                    errors.Add(new Error($"{node}: '{name}' cannot name an agent: no dots, slashes or whitespace."));
                }

                if (agent.Program is null)
                {
                    errors.Add(new Error($"{node}: agent '{name}' names no 'program'."));
                }

                foreach (var (variable, value) in agent.Environment)
                {
                    if (value.StartsWith("op://", StringComparison.Ordinal) && !SecretReference.TryFrom(value, out _))
                    {
                        errors.Add(new Error($"{node}: agent '{name}' sets {variable} to '{value}', which is not op://<vault>/<item>/<field>."));
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            return StepResult.Failed(errors);
        }

        log.Detail($"{nodes.Nodes.Sum(n => n.Value.Agents.Count)} agent(s) across {nodes.Nodes.Count} node(s).");
        return StepResult.Successful;
    }
}
