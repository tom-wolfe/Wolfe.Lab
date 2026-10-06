using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Ollama.Models;
using Wolfe.Lab.Application.Workflows.Ollama.Steps;
using Wolfe.Lab.Domain.Paths;
using Wolfe.Lab.Infrastructure.Agents;

namespace Wolfe.Lab.Tests.Application.Workflows.Ollama.Steps;

public class CheckModelStoreTests
{
    private static StepResult Check(string store, params (string Name, string Value)[] environment) =>
        new CheckModelStore(new ModelStore(new PhysicalDirectory(store)), Substitute.For<IWorkflowLog>())
            .Run(new AgentDeclarations(new Dictionary<string, AgentOptions>
            {
                ["ollama"] = new() { Program = HostPath.From("/opt/ollama"), Environment = environment.ToDictionary(variable => variable.Name, variable => variable.Value) }
            }));

    [Fact]
    public void Run_PassesTheStoreTheServerIsTold() =>
        Check("/Volumes/Data2/ollama/models", ("OLLAMA_MODELS", "/Volumes/Data2/ollama/models")).IsFailure.ShouldBeFalse();

    [Fact]
    public void Run_RefusesAStoreTheServerIsNotTold() =>
        Check("/Volumes/Data2/ollama/models", ("OLLAMA_MODELS", "/elsewhere")).Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message
            .ShouldBe("'models.store' is /Volumes/Data2/ollama/models, but the ollama agent's OLLAMA_MODELS is '/elsewhere': they must be one directory.");

    [Fact]
    public void Run_RefusesAServerThatIsToldNoStore() =>
        Check("/Volumes/Data2/ollama/models").Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("OLLAMA_MODELS is unset");
}
