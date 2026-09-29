namespace Wolfe.Lab.Build.Tests;

public class LabApplicationTests
{
    // Ritten's rules run over every job of every workflow as the application is built — a step
    // needing what no earlier step produces fails here, in the pull request's check, rather than
    // on the first node to run the published CLI.
    [Fact]
    public void Create_BuildsEveryWorkflowTheLabRuns() =>
        LabApplication.Create().Build().IsError.ShouldBeFalse();
}
