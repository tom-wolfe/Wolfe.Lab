using Microsoft.Extensions.DependencyInjection;
using Ritten.Docker;
using Ritten.Git;

namespace Wolfe.Lab.Tests;

/// <summary>
/// Ritten's own git and Docker clients, running the real tools, for tests that exercise them rather than a substitute.
/// Its clients are internal to it, so they come through its registrations, on <see cref="ProcessCommandRunner"/>.
/// </summary>
internal static class RealClients
{
    private static readonly ServiceProvider Services = Build();

    public static IGit Git => Services.GetRequiredService<IGit>();

    public static IDocker Docker => Services.GetRequiredService<IDocker>();

    private static ServiceProvider Build()
    {
        var builder = WorkflowApplication.CreateBuilder();
        builder.Services.AddSingleton<ICommandRunner>(new ProcessCommandRunner());
        builder.AddGit();
        builder.AddDocker();
        return builder.Services.BuildServiceProvider();
    }
}
