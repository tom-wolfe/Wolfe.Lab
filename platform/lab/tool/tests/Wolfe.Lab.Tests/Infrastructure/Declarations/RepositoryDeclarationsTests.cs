using Ritten.Engine.FileSystem;
using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Tests.Infrastructure.Declarations;

// The repository's own declarations, held to the CLI's rules whenever the CLI is: each
// component's check holds its own declaration when it changes, and this catches a CLI change that
// would refuse one that has not — a service's own entry included.
public class RepositoryDeclarationsTests
{
    private static string Checkout()
    {
        for (var directory = AppContext.BaseDirectory; directory is not null; directory = Path.GetDirectoryName(directory))
        {
            if (Directory.Exists(Path.Combine(directory, ".git")) || File.Exists(Path.Combine(directory, ".git")))
            {
                return directory;
            }
        }

        throw new InvalidOperationException($"{AppContext.BaseDirectory} is not in a checkout.");
    }

    [Fact]
    public async Task TheRepositorysDeclarationsHold() =>
        (await ServiceCatalogReader.Read(RealClients.Git, new PhysicalDirectory(Checkout()), TestContext.Current.CancellationToken))
            .Errors?.Select(problem => problem.Message).ShouldBeNull();

    // Each directory that declares components is recognised as the workflow its deployment runs,
    // so it needs no ritten.json to name it.
    [Fact]
    public async Task EachDeclaredDirectoryIsRecognisedAsItsWorkflow()
    {
        var checkout = Checkout();
        var catalog = (await ServiceCatalogReader.Read(RealClients.Git, new PhysicalDirectory(checkout), TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();
        var mismatched = new List<string>();
        foreach (var directory in catalog.Services.SelectMany(service => service.Components).Select(component => component.Directory).Distinct())
        {
            var runs = catalog.DeploymentUnitAt(directory).ShouldNotBeNull().Value.ShouldNotBeNull().Head.Workflow.Value;
            var recognised = await DeclarationFiles.WorkflowOf(new PhysicalDirectory(Path.Combine(checkout, directory.Value)), TestContext.Current.CancellationToken);
            if (recognised != runs)
            {
                mismatched.Add($"{directory}: runs {runs}, recognised as {recognised ?? "nothing"}");
            }
        }

        mismatched.ShouldBeEmpty();
    }
}
