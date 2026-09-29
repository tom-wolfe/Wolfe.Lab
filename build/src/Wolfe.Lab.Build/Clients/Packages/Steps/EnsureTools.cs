using Ritten.Git;

namespace Wolfe.Lab.Build.Clients.Packages.Steps;

/// <summary>
/// Installs the pinned version of each tool the job runs, and puts it first on the path.
/// </summary>
/// <remarks>
/// The path is this process's, which every command it starts inherits — so Ritten's clients, which
/// run <c>tofu</c> or <c>restic</c> by name, run the pinned one without knowing it. A tool the
/// manifest does not list is the node's own, as it always was; a rehearsal that would install one
/// rehearses with the node's.
/// </remarks>
[Step("ensure tools", StepKind.Work)]
internal sealed class EnsureTools(RequiredTools required, IGit git, IPackageInstaller installer, WorkflowJob job, IWorkflowReport report, IWorkflowLog log)
{
    internal const string PathVariable = "PATH";

    public async Task<StepResult> Run(CancellationToken ct = default)
    {
        if (await git.RepositoryRoot(ct) is not { } checkout)
        {
            return new Error("Not in a git checkout, where .config/lab-tools.json is read from.");
        }

        var manifest = await ToolsManifest.Read(checkout, ct);
        var errors = new List<Error>();
        foreach (var name in required.Names)
        {
            if (manifest.Tools.GetValueOrDefault(name) is not { } options)
            {
                log.Detail($"{name} is not pinned in .config/{ToolsManifest.FileName}; the node's own runs.");
                continue;
            }

            if (!Package.From(name, options, Package.Platform).TryGetValue(out var package, out var invalid))
            {
                errors.AddRange(invalid);
                continue;
            }

            var installed = await installer.Install(package, ct);
            Packages.Report(report, log, installed);
            if (installed.Outcome == PackageOutcome.WouldInstall)
            {
                continue;
            }

            var bin = package.Bin is { } nested ? Path.Combine(installed.Directory.AbsolutePath, nested) : installed.Directory.AbsolutePath;
            if (!File.Exists(Path.Combine(bin, name)))
            {
                errors.Add(new Error($"{package.Repository} {package.Tag} ({package.Asset}) has no '{name}' in {package.Bin ?? "its top level"} to run."));
                continue;
            }

            if (!job.DryRun)
            {
                GithubPackageInstaller.Executable(Path.Combine(bin, name));
            }

            Environment.SetEnvironmentVariable(PathVariable,
                $"{bin}{Path.PathSeparator}{Environment.GetEnvironmentVariable(PathVariable)}");
        }

        return errors.Count > 0 ? StepResult.Failed(errors) : StepResult.Successful;
    }
}
