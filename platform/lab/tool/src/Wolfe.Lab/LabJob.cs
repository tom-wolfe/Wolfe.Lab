using Ritten.Git;

namespace Wolfe.Lab;

/// <summary>
/// The base for every lab job: the clients any step may reach for, registered once.
/// </summary>
/// <typeparam name="TOptions">The shape of the service's <c>ritten.json</c>.</typeparam>
public abstract class LabJob<TOptions> : Job<TOptions> where TOptions : WorkflowSettings
{
    /// <inheritdoc />
    protected override void Configure(IWorkflowBuilder builder, TOptions options)
    {
        builder.AddCommandRunner();
        builder.AddGit();
        builder.AddLabConfiguration();
    }
}
