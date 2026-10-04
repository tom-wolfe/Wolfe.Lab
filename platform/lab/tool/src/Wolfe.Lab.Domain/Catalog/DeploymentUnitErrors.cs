using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Paths;

namespace Wolfe.Lab.Domain.Catalog;

/// <summary>
/// The well-known problems with what a directory declares, taken together.
/// </summary>
public static class DeploymentUnitErrors
{
    /// <summary>
    /// Nothing in the catalog is declared in the directory.
    /// </summary>
    public static Error NothingDeclared(RepositoryPath directory) =>
        new($"{directory} declares no component: a component is a document in a component.yaml beside what deploys it, of the service its parent's service.yaml declares (platform/lab/README.md, \"Declarations\").");

    /// <summary>
    /// The directory declares other than one component of a workflow that deploys exactly one.
    /// </summary>
    public static Error NotOneOf(RepositoryPath directory, WorkflowName workflow, int count) =>
        new($"{directory} declares {count} {workflow} components, and the {workflow} workflow deploys exactly one.");
}
