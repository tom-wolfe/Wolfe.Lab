using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Application;

/// <summary>
/// The base for every lab workflow: one runs in a directory whose components declare it, with no
/// <c>ritten.json</c> to name it.
/// </summary>
public abstract class LabWorkflow : IWorkflow
{
    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public abstract string Label { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<IJob> Jobs { get; }

    /// <inheritdoc />
    public async Task<string?> IsCompatible(IDirectory directory, CancellationToken cancellationToken = default) =>
        await DeclarationFiles.WorkflowOf(directory, cancellationToken) == Name ? $"its components declare the {Label} workflow" : null;
}
