using Wolfe.Lab.Domain.Catalog;
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

    // The committed schema is what every editor reads, so it must be the one the CLI judges by. A
    // local build writes it (Wolfe.Lab.csproj, WriteLabSchema); in CI, which does not, this is
    // what catches a branch that changed the declarations' types without building.
    [Fact]
    public void TheCommittedSchemaIsTheOneTheTypesGenerate()
    {
        var file = Path.Combine(Checkout(), LabSchema.Location.Value);

        File.Exists(file).ShouldBeTrue($"{LabSchema.Location} is not committed: build the CLI locally, which writes it.");
        File.ReadAllText(file).ShouldBe(LabSchema.Json, $"{LabSchema.Location} is stale: build the CLI locally, which writes it.");
    }

    private static IReadOnlyList<string> Judge(string yaml) =>
        [.. LabSchema.Validate(YamlDocuments.Parse(yaml).Value.ShouldNotBeNull().Documents.Single()).Select(problem => $"{problem.Line}: {problem.Problem.Message}")];

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

        Judge("name: server\nkind: app\nworkflow: docker\nservice: server\ndescription: The server.\n").ShouldBeEmpty();
    }

    [Fact]
    public void Judge_SaysWhatIsMissingAndWhere() =>
        Judge("kind: service\nname: immich\n").ShouldBe(["1: Required properties [\"description\"] are not present"]);

    [Fact]
    public void Judge_RefusesAKeyNothingDeclaresInItsOwnWords() =>
        Judge("name: server\nkind: app\nworkflow: docker\nservice: server\nvolumes: [/Volumes/Data2]\n").ShouldBe(["5: volumes: 'volumes' is not something a docker component declares."]);

    [Fact]
    public void Judge_HoldsEachProblemAsTheWellKnownErrorOfItsField() =>
        LabSchema.Validate(YamlDocuments.Parse("name: server\nkind: app\nworkflow: docker\nservice: server\nvolumes: [/Volumes/Data2]\n").Value.ShouldNotBeNull().Documents.Single())
            .ShouldHaveSingleItem().Problem.ShouldBe(new FieldError("volumes", DeclarationErrors.NotDeclarable("volumes", "a docker component")));

    [Fact]
    public void Judge_RefusesANameThatIsNotOneInItsOwnWords() =>
        Judge("kind: service\nname: Immich\ndescription: Photos.\n")
            .ShouldBe(["2: name: 'Immich' is not a name: lower case, a letter first, then letters, digits and hyphens."]);

    [Fact]
    public void Judge_RefusesAWorkflowThatOperatesNoComponent() =>
        Judge("kind: app\nworkflow: gatus-health\n").ShouldHaveSingleItem()
            .ShouldStartWith("2: workflow: 'gatus-health' is not a workflow that operates components (docker, dotnet-service, agent,");

    // Kind and workflow are independent: any usage, operated by any workflow.
    [Theory]
    [InlineData("name: broker\nkind: queue\nworkflow: tofu\n")]
    [InlineData("name: cache\nkind: cache\nworkflow: docker\nservice: redis\n")]
    public void Judge_TakesAnyKindWithAnyWorkflow(string yaml) =>
        Judge(yaml).ShouldBeEmpty();

    [Fact]
    public void Judge_HoldsADockerComponentToItsService() =>
        Judge("name: server\nkind: app\nworkflow: docker\n").ShouldBe(["1: Required properties [\"service\"] are not present"]);

    [Fact]
    public void Judge_RefusesAKindTheLabDoesNotHave() =>
        Judge("kind: daemon\nworkflow: docker\n").ShouldHaveSingleItem().ShouldStartWith("1: kind: 'daemon' is not a kind of declaration (service, node, app, backend, database,");

    [Fact]
    public void Judge_SaysWhichLifecyclesAndLinkTypesThereAre() =>
        Judge("kind: service\nname: immich\ndescription: Photos.\nlifecycle: live\nlinks:\n  - { title: Home, type: homepage, url: https://immich.app }\n")
            .ShouldBe(["4: lifecycle: 'live' is not a lifecycle (production, experimental, deprecated).",
                "6: links.0.type: 'homepage' is not a type of link (app, runbook, docs, dashboard, repository)."]);

    [Fact]
    public void Judge_SaysWhatIsWrongWithAMetricsEndpoint_NotThatItIsNotAList() =>
        Judge("name: server\nkind: app\nworkflow: docker\nservice: server\nmetrics: { port: 70000 }\n")
            .ShouldBe(["5: metrics.port: 70000 should be at most 65535"]);

    [Fact]
    public void Judge_StillRefusesMetricsThatAreNeitherAnEndpointNorAList() =>
        Judge("name: server\nkind: app\nworkflow: docker\nservice: server\nmetrics: lots\n").ShouldNotBeEmpty();
}
