using Ritten.Contracts;
using Wolfe.Lab.Domain.Catalog.Components;

namespace Wolfe.Lab.Domain.Extensions;

/// <summary>
/// Contains extensions for <see cref="WorkflowName"/>.
/// </summary>
public static class WorkflowJobExtensions
{
    extension (WorkflowJob job)
    {
        /// <summary>
        /// Gets the domain-safe name of the workflow.
        /// </summary>
        public WorkflowName WorkflowName => WorkflowName.From(job.Workflow);
    }
}
