namespace Wolfe.Lab.Build.Clients.Agents.Steps;

/// <summary>
/// Turns what the slice declared into what this node can run.
/// </summary>
[Step("resolve agents", StepKind.Work)]
internal sealed class ResolveAgents(AgentDeclarations declarations, IWorkflowLog log)
{
    public StepResult<AgentPlan> Run()
    {
        var resolved = new List<AgentDefinition>();
        var errors = new List<Error>();

        foreach (var (name, settings) in declarations.Agents.OrderBy(agent => agent.Key, StringComparer.Ordinal))
        {
            if (AgentLabel.ForName(name) is not { } label)
            {
                errors.Add(new Error($"'{name}' cannot name an agent: no dots, slashes or whitespace."));
                continue;
            }

            if (settings.Program is not { } program)
            {
                errors.Add(new Error($"Agent '{name}' names no 'program' in ritten.json."));
                continue;
            }

            if (!File.Exists(program.Value))
            {
                errors.Add(new Error($"Agent '{name}' runs {program.Value}, which is not on this node."));
                continue;
            }

            if (settings.ToDefinition(label, File.GetLastWriteTimeUtc(program.Value)) is not { } definition)
            {
                errors.Add(new Error($"Agent '{name}' is incomplete."));
                continue;
            }

            resolved.Add(definition);
        }

        if (errors.Count > 0)
        {
            return StepResult.Failed(errors);
        }

        log.Detail($"Resolved {resolved.Count} agent{(resolved.Count == 1 ? "" : "s")}.");
        return new AgentPlan(resolved);
    }
}
