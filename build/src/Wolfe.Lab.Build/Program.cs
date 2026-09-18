using System.CommandLine;
using Ritten.CommandLine;
using Wolfe.Lab.Build.Files.Workflows;
using Wolfe.Lab.Build.Immich.Workflows;
using Wolfe.Lab.Build.Obsidian.Workflows;
using Wolfe.Lab.Build.Runtimes;

var builder = WorkflowApplication.CreateBuilder();

builder.Workflows
    .Add<ObsidianWorkflow>()
    .Add<ImmichWorkflow>()
    .Add<FilesWorkflow>();

builder.Runtimes
    .Add<LabRuntime>();

var built = builder.Build();
if (built.IsError)
{
    return ExitCode.ConfigurationError;
}

var root = new RootCommand("The lab's jobs. The workflow to run is declared by the ritten.json in the working directory.");
await root.InstallRitten(built.Value);

return await root.Parse(args).InvokeAsync();
