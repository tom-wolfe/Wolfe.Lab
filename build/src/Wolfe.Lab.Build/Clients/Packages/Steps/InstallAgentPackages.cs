using Wolfe.Lab.Build.Clients.Agents;
using Wolfe.Lab.Build.Clients.Agents.Steps;
using Wolfe.Lab.Build.Clients.Releases;

namespace Wolfe.Lab.Build.Clients.Packages.Steps;

/// <summary>
/// Installs what each agent runs, when it declares a <c>package</c>, before the agents are resolved.
/// </summary>
/// <remarks>
/// The lab runs the agent, so it installs it too: its version pinned beside its unit, not left to
/// whatever Homebrew last upgraded to. An agent without a package runs what the node already has.
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
            if (agent.Package is not { } settings)
            {
                continue;
            }

            if (!Package.From(name, settings, Package.Platform).TryGetValue(out var package, out var invalid))
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
        }

        return errors.Count > 0 ? StepResult.Failed(errors) : new AgentPackages(installed);
    }
}
