using Ritten.Engine.FileSystem;
using Wolfe.Lab.Application.Workflows.Docker.Steps;
using Wolfe.Lab.Tests.Domain.Catalog;

namespace Wolfe.Lab.Tests.Application.Workflows.Docker.Steps;

public class CheckComposeBindingsTests
{
    private readonly ICommandRunner _commands = Substitute.For<ICommandRunner>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public CheckComposeBindingsTests()
    {
        _fileSystem.ProjectRoot.Returns(new PhysicalDirectory(Path.GetTempPath()));
        _commands.Run(Arg.Any<Command>(), Arg.Any<CancellationToken>())
            .Returns(new CommandResult(0, """{ "services": { "immich-server": { "image": "server" } } }""", ""));
    }

    private Task<StepResult> Run(string service) =>
        new CheckComposeBindings(_commands, _fileSystem, Substitute.For<IWorkflowLog>())
            .Run(Catalogs.Unit("personal/immich/compose", Catalogs.Compose("server", service)), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Run_PassesComponentsThatBindTheWholeStack() =>
        (await Run("immich-server")).IsFailure.ShouldBeFalse();

    [Fact]
    public async Task Run_RefusesAStackTheComponentsDoNotFit_AtTheirDeclaration() =>
        (await Run("immich-sever")).Errors.ShouldNotBeNull().Select(error => error.Message).ShouldBe([
            "personal/immich/compose/component.yaml: service: the compose stack has no service 'immich-sever' (immich-server).",
            "the compose stack's service 'immich-server' is no component's: declare one in personal/immich/compose, with service: immich-server."
        ]);
}
