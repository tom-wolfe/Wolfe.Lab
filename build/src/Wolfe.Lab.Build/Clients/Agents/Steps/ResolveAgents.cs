using Wolfe.Lab.Build.Clients.Secrets;

namespace Wolfe.Lab.Build.Clients.Agents.Steps;

/// <summary>
/// Turns what the slice declared into what this node can run.
/// </summary>
/// <remarks>
/// An environment value that is a vault reference — <c>op://vault/item/field</c> — is resolved
/// here, so a secret reaches the agent the way it reaches a container: through the deploy, never
/// through a file in the repository. It lands in the unit, which is written for the owner alone.
/// </remarks>
[Step("resolve agents", StepKind.Work)]
internal sealed class ResolveAgents(AgentDeclarations declarations, ISecretProvider secrets, IWorkflowLog log)
{
    public async Task<StepResult<AgentPlan>> Run(CancellationToken ct = default)
    {
        if (declarations.Agents.Count == 0)
        {
            return new Error("No agents are declared here: a deploy that converges nothing is a mistake, not a success.");
        }

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

            if (UnexpandedHomePaths(settings.Environment).ToList() is { Count: > 0 } unexpanded)
            {
                errors.Add(new Error($"Agent '{name}' sets {string.Join(", ", unexpanded)} to a path under ~, which nothing expands: "
                                     + "the process would receive the ~ as written. Write the absolute path."));
                continue;
            }

            if (settings.ToDefinition(label, File.GetLastWriteTimeUtc(program.Value)) is not { } definition)
            {
                errors.Add(new Error($"Agent '{name}' is incomplete."));
                continue;
            }

            resolved.Add(definition with { Environment = await Resolve(definition.Environment, ct) });
        }

        if (errors.Count > 0)
        {
            return StepResult.Failed(errors);
        }

        log.Detail($"Resolved {resolved.Count} agent{(resolved.Count == 1 ? "" : "s")}.");
        return new AgentPlan(resolved);
    }

    private async Task<IReadOnlyDictionary<string, string>> Resolve(IReadOnlyDictionary<string, string> environment, CancellationToken ct)
    {
        var resolved = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (name, value) in environment)
        {
            resolved[name] = SecretReference.TryFrom(value, out var reference) ? await secrets.Resolve(reference.Value, ct) : value;
        }

        return resolved;
    }

    /// <summary>
    /// The variables whose value starts with <c>~</c>. A path setting — <c>program</c>, <c>log</c> —
    /// is expanded when it is read; an environment value is handed to the process as written, and
    /// neither the supervisor nor most programs expand it.
    /// </summary>
    internal static IEnumerable<string> UnexpandedHomePaths(IReadOnlyDictionary<string, string> environment) =>
        environment.Where(variable => variable.Value == "~" || variable.Value.StartsWith("~/", StringComparison.Ordinal))
            .Select(variable => variable.Key)
            .Order(StringComparer.Ordinal);
}
