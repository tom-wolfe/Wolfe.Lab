using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Workflows.Ollama.Models;
using Wolfe.Lab.Build.Workflows.Ollama.Steps;
using OllamaModel = Wolfe.Lab.Build.Clients.Ollama.OllamaModel;

namespace Wolfe.Lab.Build.Tests.Workflows.Ollama.Steps;

public class CheckRolesTests : IDisposable
{
    private readonly DirectoryInfo _slice = Directory.CreateTempSubdirectory("lab-ollama-");
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public CheckRolesTests() =>
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(Path.Combine(_slice.FullName, "server")));

    public void Dispose() => _slice.Delete(recursive: true);

    private void Component(string name, string json) =>
        File.WriteAllText(Path.Combine(_slice.CreateSubdirectory(name).FullName, "ritten.json"), json);

    private static string Ollama(string embedding) => $$"""
        {
          // Comments and trailing commas, as every component's file may have.
          "workflow": "ollama",
          "models": {
            "pull": ["{{embedding}}"],
            "roles": { "embedding": { "model": "{{embedding}}", "identical": true } },
          }
        }
        """;

    private CheckRoles Step(string embedding) => new(
        new DeclaredRoles(new ModelSettings
        {
            Pull = [OllamaModel.From(embedding)],
            Roles = new Dictionary<string, ModelRole> { ["embedding"] = new() { Model = OllamaModel.From(embedding), Identical = true } }
        }),
        _fileSystem,
        Substitute.For<IWorkflowLog>());

    [Fact]
    public void Run_PassesWhenEveryServerAgrees()
    {
        Component("server", Ollama("embeddinggemma:300m"));
        Component("studio", Ollama("embeddinggemma:300m"));

        Step("embeddinggemma:300m").Run().IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void Run_FailsWhenASiblingDisagrees()
    {
        Component("server", Ollama("embeddinggemma:300m"));
        Component("studio", Ollama("nomic-embed-text:v1.5"));

        Step("embeddinggemma:300m").Run().IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Run_IgnoresComponentsOfAnotherWorkflow()
    {
        Component("server", Ollama("embeddinggemma:300m"));
        Component("backup", """{ "workflow": "backup" }""");

        Step("embeddinggemma:300m").Run().IsFailure.ShouldBeFalse();
    }
}
