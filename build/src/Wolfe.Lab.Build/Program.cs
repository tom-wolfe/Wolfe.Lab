using System.CommandLine;
using Ritten.CommandLine;
using Wolfe.Lab.Build.Obsidian.Workflows;

var builder = WorkflowApplication.CreateBuilder();

builder.Workflows
    .Add<ObsidianWorkflow>();

var built = builder.Build();
if (built.IsError)
{
    return ExitCode.ConfigurationError;
}

var root = new RootCommand("The lab's jobs. The workflow to run is declared by the ritten.json in the working directory.");
await root.InstallRitten(built.Value);

return await root.Parse(args).InvokeAsync();
