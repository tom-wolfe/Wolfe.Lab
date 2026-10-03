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
        (await DeclarationReader.Read(new ProcessCommandRunner(), Checkout(), TestContext.Current.CancellationToken))
            .Errors.Select(problem => problem.Message).ShouldBeEmpty();
}
