using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Telemetry;

namespace Wolfe.Lab.Tests.Application.Telemetry;

// Every component in the repository, held to the list the CLI holds. A component's own check runs
// only when that component changes; this runs whenever the CLI does, so a new attribute fails
// here until every store is told about it, rather than on whichever pull request touches it next.
public class RepositoryTelemetryTests
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

    public static TheoryData<string> Components()
    {
        var checkout = Checkout();
        var data = new TheoryData<string>();
        foreach (var declaration in Directory.EnumerateFiles(checkout, "ritten.json", SearchOption.AllDirectories)
                     .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj" or "node_modules" || part.StartsWith('.')))
                     .Order(StringComparer.Ordinal))
        {
            data.Add(Path.GetRelativePath(checkout, Path.GetDirectoryName(declaration) ?? checkout));
        }

        // Not a component, but it declares a log file to the collector: the runner's.
        data.Add(Path.Combine("platform", "chezmoi", "home"));
        return data;
    }

    [Theory]
    [MemberData(nameof(Components))]
    public async Task EveryTelemetryNameIsOneTheLabKnows(string component) =>
        (await CheckTelemetryNames.Check(RealClients.Git, RealClients.Docker, new PhysicalDirectory(Path.Combine(Checkout(), component)), TestContext.Current.CancellationToken))
            .Select(error => error.Message).ShouldBeEmpty();
}
