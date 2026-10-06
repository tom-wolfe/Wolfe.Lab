using Wolfe.Lab.Application.Agents;
using Wolfe.Lab.Domain;
using Wolfe.Lab.Infrastructure.Agents;
using Wolfe.Lab.Infrastructure.Packages;

namespace Wolfe.Lab.Application.Packages;

/// <summary>
/// Installs the packages each agent needs to run, when it declares a <c>package</c>, before the agents are resolved.
/// </summary>
[Step("install agent packages", StepKind.Work)]
internal sealed class InstallAgentPackages(IPackageInstaller installer, AgentResolver agents, EnvironmentPath path, WorkflowJob job, IWorkflowReport report, IWorkflowLog log)
{
    public async Task<StepResult<AgentPackages>> Run(AgentDeclarations declarations, CancellationToken ct = default)
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
            var directory = result.Directory;
            if (!job.DryRun
                && agents.Expand(agent, directory).Program is { File: var program }
                && directory.RelativePath(program) is var relative && !relative.StartsWith("../", StringComparison.Ordinal) && !Path.IsPathRooted(relative)
                && program.Exists)
            {
                GithubPackageInstaller.Executable(program);
            }

            // What the job runs of the agent's own — ollama's `pull` against ollama's server —
            // is the version the agent runs, not whatever else the node has.
            if (!job.DryRun)
            {
                path.Prepend(directory);
            }
        }

        return errors.Count > 0 ? StepResult.Failed(errors) : new AgentPackages(installed);
    }
}
