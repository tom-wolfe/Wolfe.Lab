using Wolfe.Lab.Clients.Releases;

namespace Wolfe.Lab.Tests.Clients.Releases;

public class LabRootsTests
{
    private static readonly LabRoots Roots = new("/lab/root", "/lab/data");

    [Fact]
    public void From_ReadsBothRootsFromTheEnvironment()
    {
        var roots = LabRoots.From(new WorkflowEnvironment(name => name switch { "LAB_ROOT" => "/r", "LAB_DATA" => "/d", _ => null }));

        roots.ShouldBe(new LabRoots("/r", "/d"));
    }

    [Fact]
    public void From_FallsBackToWhatTheyHaveAlwaysBeen()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        LabRoots.From(new WorkflowEnvironment(_ => null))
            .ShouldBe(new LabRoots(Path.Combine(home, ".local", "share", "Wolfe.Lab"), Path.Combine(home, "Docker")));
    }

    [Fact]
    public void Expand_ReplacesTheLabsRootsAndNothingElse() =>
        Roots.Expand("${LAB_ROOT}/alloy:${LAB_DATA}/x:${HOME}").ShouldBe("/lab/root/alloy:/lab/data/x:${HOME}");

    [Theory]
    [InlineData("/lab/root/alloy", true)]
    [InlineData("/lab/root/a/b", true)]
    [InlineData("/lab/root", false)]
    [InlineData("/lab/rootless", false)]
    [InlineData("/lab/root/../elsewhere", false)]
    [InlineData("/Users/tomwolfe", false)]
    public void Contains_IsStrictlyInsideTheInstallRoot(string path, bool expected) =>
        Roots.Contains(path).ShouldBe(expected);
}
