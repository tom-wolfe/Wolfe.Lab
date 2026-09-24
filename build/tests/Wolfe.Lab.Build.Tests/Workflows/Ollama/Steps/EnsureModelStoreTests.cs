using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Workflows.Ollama.Models;
using Wolfe.Lab.Build.Workflows.Ollama.Steps;

namespace Wolfe.Lab.Build.Tests.Workflows.Ollama.Steps;

public class EnsureModelStoreTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("lab-models-");

    public void Dispose() => _root.Delete(recursive: true);

    private string Store => Path.Combine(_root.FullName, "models");

    private EnsureModelStore Step(bool dryRun) =>
        new(new ModelStore(new PhysicalDirectory(Store)), new WorkflowJob("ollama", "deploy", dryRun, AutoApprove: true), Substitute.For<IWorkflowLog>());

    [Fact]
    public void Run_CreatesTheStore()
    {
        Step(dryRun: false).Run().IsFailure.ShouldBeFalse();

        Directory.Exists(Store).ShouldBeTrue();
    }

    [Fact]
    public void Run_CreatesNothingOnADryRun()
    {
        Step(dryRun: true).Run().IsFailure.ShouldBeFalse();

        Directory.Exists(Store).ShouldBeFalse();
    }
}
