using Wolfe.Lab.Application.Packages;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Packages;
using Wolfe.Lab.Infrastructure.Releases;
using Wolfe.Lab.Infrastructure.Secrets;

namespace Wolfe.Lab.Application.Agents;

/// <summary>
/// Turns what the service declared into what this node can run.
/// </summary>
/// <remarks>
/// An environment value that is a vault reference — <c>op://vault/item/field</c> — is resolved
/// here, so a secret reaches the agent the way it reaches a container: through the deploy, never
/// through a file in the repository. It lands in the unit, which is written for the owner alone.
/// <c>${LAB_ROOT}</c>, <c>${LAB_DATA}</c> and <c>${PACKAGE}</c> are expanded in every path, argument
/// and variable first, so an agent can name its artifacts and its program wherever this node keeps them.
/// </remarks>
[Step("resolve agents", StepKind.Work)]
internal sealed class ResolveAgents(ISecretProvider secrets, IOptions<LabDirectories> options, IWorkflowLog log)
{
    /// <summary>
    /// The agent's installed package, in any of its options.
    /// </summary>
    private const string PackageVariable = "${PACKAGE}";

    /// <summary>
    /// The agent's installed package, as a catalog declaration writes it: the one placeholder only
    /// the install can expand.
    /// </summary>
    private const string PackagePlaceholder = "{package}";

    public async Task<StepResult<AgentPlan>> Run(AgentDeclarations declarations, AgentPackages packages, CancellationToken ct = default)
    {
        if (declarations.Agents.Count == 0)
        {
            return new Error("No agents are declared here: a deploy that converges nothing is a mistake, not a success.");
        }

        var roots = options.Value;
        var resolved = new List<AgentDefinition>();
        var errors = new List<Error>();

        foreach (var (name, declared) in declarations.Agents.OrderBy(agent => agent.Key, StringComparer.Ordinal))
        {
            var package = packages.Packages.GetValueOrDefault(name);
            var options = Expand(declared, roots, package?.Directory.AbsolutePath);
            if (AgentLabel.ForName(name) is not { } label)
            {
                errors.Add(new Error($"'{name}' cannot name an agent: no dots, slashes or whitespace."));
                continue;
            }

            if (options.Program is not { } program)
            {
                errors.Add(new Error($"Agent '{name}' names no 'program' in ritten.json."));
                continue;
            }

            // A rehearsal installs nothing, so a package it would install is not there to find.
            var rehearsed = package?.Outcome == PackageOutcome.WouldInstall;
            if (!rehearsed && !program.File.Exists)
            {
                errors.Add(new Error(package is null
                    ? $"Agent '{name}' runs {program.Value}, which is not on this node."
                    : $"Agent '{name}' runs {program.Value}, which its package {package.Package.Repository} {package.Package.Tag} does not contain."));
                continue;
            }

            if (UnexpandedHomePaths(options.Environment).ToList() is { Count: > 0 } unexpanded)
            {
                errors.Add(new Error($"Agent '{name}' sets {string.Join(", ", unexpanded)} to a path under ~, which nothing expands: "
                                     + "the process would receive the ~ as written. Write the absolute path."));
                continue;
            }

            if (options.ToDefinition(label, rehearsed ? DateTimeOffset.UnixEpoch : System.IO.File.GetLastWriteTimeUtc(program.File.AbsolutePath)) is not { } definition)
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

    /// <summary>
    /// The options with the lab's roots written in.
    /// </summary>
    internal static AgentOptions Expand(AgentOptions options, LabDirectories directories, string? package = null)
    {
        return options with
        {
            Program = Path(options.Program),
            Arguments = [.. options.Arguments.Select(Value)],
            Environment = options.Environment.ToDictionary(variable => variable.Key, variable => Value(variable.Value), StringComparer.Ordinal),
            WorkingDirectory = Path(options.WorkingDirectory),
            Log = Path(options.Log)
        };

        HostPath? Path(HostPath? path) => path is { } value ? HostPath.From(Value(value.Value)) : null;

        string Value(string value) => directories.Expand(package is null
            ? value
            : value.Replace(PackageVariable, package, StringComparison.Ordinal).Replace(PackagePlaceholder, package, StringComparison.Ordinal));
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
