using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Packages;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Application.Packages;

/// <summary>
/// Installs what each agent runs, when it declares a <c>package</c>, before the agents are resolved.
/// </summary>
/// <remarks>
/// The lab runs the agent, so it installs it too: its version pinned beside its unit, not left to
/// whatever Homebrew last upgraded to. An agent without a package runs what the node already has.
/// The package goes first on the job's path as well, so a command the job itself runs by name is
/// the agent's version.
/// </remarks>
[Step("install agent packages", StepKind.Work)]
internal sealed class InstallAgentPackages(AgentDeclarations declarations, IPackageInstaller installer, WorkflowEnvironment environment, WorkflowJob job, IWorkflowReport report, IWorkflowLog log)
{
    public async Task<StepResult<AgentPackages>> Run(CancellationToken ct = default)
    {
        var installed = new Dictionary<string, InstalledPackage>(StringComparer.Ordinal);
        var errors = new List<Error>();
        foreach (var (name, agent) in declarations.Agents.OrderBy(agent => agent.Key, StringComparer.Ordinal))
        {
            if (agent.Package is not { } options)
            {
                continue;
            }

            if (!Package.From(name, options, Package.Platform).TryGetValue(out var package, out var invalid))
            {
                errors.AddRange(invalid);
                continue;
            }

            var result = await installer.Install(package, ct);
            installed[name] = result;
            Packages.Report(report, log, result);

            // The program the agent runs out of its package, which the release may not have
            // marked executable.
            var directory = result.Directory.AbsolutePath;
            if (!job.DryRun
                && ResolveAgents.Expand(agent, LabRoots.From(environment), directory).Program is { } program
                && program.Value.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && File.Exists(program.Value))
            {
                GithubPackageInstaller.Executable(program.Value);
            }

            // What the job runs of the agent's own — ollama's `pull` against ollama's server —
            // is the version the agent runs, not whatever else the node has.
            if (!job.DryRun)
            {
                Environment.SetEnvironmentVariable(EnsureTools.PathVariable,
                    $"{directory}{Path.PathSeparator}{Environment.GetEnvironmentVariable(EnsureTools.PathVariable)}");
            }
        }

        return errors.Count > 0 ? StepResult.Failed(errors) : new AgentPackages(installed);
    }
}
