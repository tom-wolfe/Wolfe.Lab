using Wolfe.Lab.Domain.Catalog.Components;

namespace Wolfe.Lab.Application.Catalog;

/// <summary>
/// The well-known ways a component's declared workflow and its directory's <c>ritten.json</c> disagree.
/// </summary>
internal static class WorkflowErrors
{
    /// <summary>
    /// The component declares a workflow other than the one its directory runs.
    /// </summary>
    public static Error NotOwnedByTheDirectory(WorkflowName declared, string runs) =>
        new($"declares the {declared} workflow, but its directory's ritten.json runs {runs}.");
}
