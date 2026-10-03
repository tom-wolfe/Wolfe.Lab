using Wolfe.Lab.Infrastructure.Declarations;

namespace Wolfe.Lab.Tests.Infrastructure.Declarations;

public class LabSchemaTests
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

    // The committed schema is what every editor reads, so it must be the one the CLI judges by.
    // LAB_WRITE_SCHEMA=1 writes it, rather than anyone writing it by hand.
    [Fact]
    public void TheCommittedSchemaIsTheOneTheTypesGenerate()
    {
        var file = Path.Combine(Checkout(), LabSchema.Location.Value);
        if (Environment.GetEnvironmentVariable("LAB_WRITE_SCHEMA") == "1")
        {
            File.WriteAllText(file, LabSchema.Json);
        }

        File.Exists(file).ShouldBeTrue($"{LabSchema.Location} is not committed: run the tests with LAB_WRITE_SCHEMA=1.");
        File.ReadAllText(file).ShouldBe(LabSchema.Json, $"{LabSchema.Location} is stale: run the tests with LAB_WRITE_SCHEMA=1.");
    }

    private static IReadOnlyList<string> Judge(string yaml) =>
        [.. LabSchema.Judge(YamlDocuments.Parse(yaml).Value.ShouldNotBeNull().Documents.Single()).Select(problem => $"{problem.Line}: {problem.Problem}")];

    [Fact]
    public void Judge_PassesAServiceAndAComponent()
    {
        Judge("""
            kind: service
            name: immich
            description: The photo library.
            links:
              - { title: Runbook, type: runbook, path: RUNBOOK.md }
            """).ShouldBeEmpty();

        Judge("kind: workload\ntype: compose\ndescription: The stack.\n").ShouldBeEmpty();
    }

    [Fact]
    public void Judge_SaysWhatIsMissingAndWhere() =>
        Judge("kind: service\nname: immich\n").ShouldBe(["1: the document: Required properties [\"description\"] are not present"]);

    [Fact]
    public void Judge_RefusesAKeyNothingDeclaresInItsOwnWords() =>
        Judge("kind: workload\ntype: compose\nvolumes: [/Volumes/Data2]\n").ShouldBe(["3: volumes: 'volumes' is not something a workload declares."]);

    [Fact]
    public void Judge_RefusesANameThatIsNotOneInItsOwnWords() =>
        Judge("kind: service\nname: Immich\ndescription: Photos.\n")
            .ShouldBe(["2: name: 'Immich' is not a name: lower case, a letter first, then letters, digits and hyphens."]);

    [Fact]
    public void Judge_HoldsAKindToItsOwnTypes() =>
        Judge("kind: workload\ntype: snapshot\n").ShouldBe(["2: type: 'snapshot' is not a type of workload (compose, agent)."]);

    [Fact]
    public void Judge_RefusesAKindTheLabDoesNotHave() =>
        Judge("kind: daemon\ntype: compose\n").ShouldHaveSingleItem().ShouldStartWith("1: kind: 'daemon' is not a kind of declaration (service, workload, backup,");

    [Fact]
    public void Judge_SaysWhichLifecyclesAndLinkTypesThereAre() =>
        Judge("kind: service\nname: immich\ndescription: Photos.\nlifecycle: live\nlinks:\n  - { title: Home, type: homepage, url: https://immich.app }\n")
            .ShouldBe(["4: lifecycle: 'live' is not a lifecycle (production, experimental, deprecated).",
                "6: links.0.type: 'homepage' is not a type of link (app, runbook, docs, dashboard, repository)."]);
}
