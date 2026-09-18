using Ritten.Docker;
using Ritten.Engine.FileSystem;
using Wolfe.Lab.Build.Http;
using Wolfe.Lab.Build.Immich.Models;
using Wolfe.Lab.Build.Immich.Steps;
using Wolfe.Lab.Build.Secrets;

namespace Wolfe.Lab.Build.Tests.Immich.Steps;

public class ImportTakeoutTests
{
    private static readonly SecretReference Key = SecretReference.From("op://Wolfe.Lab/immich-api-key/credential");
    private readonly IDocker _docker = Substitute.For<IDocker>();
    private readonly ISecrets _secrets = Substitute.For<ISecrets>();

    [Fact]
    public async Task Run_StartsImmichGoOnTheLabNetworkWithTheKeyInItsEnvironment()
    {
        _secrets.Read(Key, Arg.Any<CancellationToken>()).Returns("k3y");
        var step = new ImportTakeout(
            _docker,
            _secrets,
            new ImmichServer(ServiceUrl.From("http://immich-server:2283"), Key),
            new ImportOptions(4),
            Substitute.For<IWorkflowLog>());
        var takeout = new Takeout(new PhysicalDirectory("/Volumes/Data2/photos/google"), ["takeout-001.zip", "takeout-002.zip"]);

        var result = await step.Run(takeout, new ImmichGoImage("lab/immich-go", new PhysicalDirectory("/lab/immich/immich-go")), TestContext.Current.CancellationToken);

        result.IsFailure.ShouldBeFalse();
        var run = (ContainerRun)_docker.ReceivedCalls().Single().GetArguments()[0]!;
        run.Image.ShouldBe("lab/immich-go");
        run.Network.ShouldBe(ImportTakeout.Network);
        run.Arguments.ShouldBe(["upload", "from-google-photos", "--concurrent-tasks", "4", "--pause-immich-jobs", "--session-tag", "--include-unmatched", "--no-ui", "/takeout/takeout-001.zip", "/takeout/takeout-002.zip"]);
        run.Mounts.ShouldHaveSingleItem().ShouldBe(new BindMount(takeout.Directory, ImportTakeout.MountPoint, ReadOnly: true));
        run.Environment["IMMICH_GO_UPLOAD_SERVER"].ShouldBe("http://immich-server:2283");
        run.Environment["IMMICH_GO_UPLOAD_API_KEY"].ShouldBe("k3y");
    }
}
