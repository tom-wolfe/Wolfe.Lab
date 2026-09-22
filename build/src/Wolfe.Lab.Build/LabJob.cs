using Ritten.Git;

namespace Wolfe.Lab.Build;

/// <summary>
/// The base for every lab job: the clients any step may reach for, registered once.
/// </summary>
/// <typeparam name="TSettings">The shape of the slice's <c>ritten.json</c>.</typeparam>
public abstract class LabJob<TSettings> : Job<TSettings> where TSettings : WorkflowSettings
{
    /// <inheritdoc />
    protected override void Configure(IWorkflowBuilder builder, TSettings settings)
    {
        builder.AddCommandRunner();
        builder.AddGit();
    }
}
