using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Tests.Domain.Catalog;

public class RepositoryPathTests
{
    private static RepositoryPath Path(string path) => RepositoryPath.From(path);

    [Theory]
    [InlineData(@"personal\immich\service.yaml", "personal/immich/service.yaml")]
    [InlineData("personal/immich/", "personal/immich")]
    public void From_WritesItTheRepositorysWay(string written, string path) => Path(written).Value.ShouldBe(path);

    [Theory]
    [InlineData("")]
    [InlineData("/Users/tomwolfe")]
    [InlineData("C:/lab")]
    [InlineData("personal/../platform")]
    [InlineData("./personal")]
    [InlineData("personal//immich")]
    public void From_RefusesWhatIsNotAPathInTheRepository(string written) => RepositoryPath.TryFrom(written).IsSuccess.ShouldBeFalse();

    [Fact]
    public void Parent_AndDirectories_WalkUpTheTree()
    {
        var file = Path("personal/immich/compose/component.yaml");

        file.Directories.ShouldBe(["personal", "immich", "compose"]);
        file.Parent.ShouldBe(Path("personal/immich/compose"));
        Path("README.md").Parent.ShouldBeNull();
    }

    [Fact]
    public void Contains_IsWithinTheDirectory_NotStartsWithItsName()
    {
        var compose = Path("personal/immich/compose");

        compose.Contains(Path("personal/immich/compose/component.yaml")).ShouldBeTrue();
        Path("personal/immich/comp").Contains(Path("personal/immich/compose/component.yaml")).ShouldBeFalse();
        compose.Contains(compose).ShouldBeFalse();
    }

    [Theory]
    [InlineData("personal/immich", "RUNBOOK.md", "personal/immich/RUNBOOK.md")]
    [InlineData("personal/immich/compose", "../../../platform/lab/schema/lab.schema.json", "platform/lab/schema/lab.schema.json")]
    [InlineData("personal/immich", "./docs/../RUNBOOK.md", "personal/immich/RUNBOOK.md")]
    [InlineData(null, "platform/lab", "platform/lab")]
    public void Resolve_FollowsARelativePath(string? from, string relative, string expected) =>
        RepositoryPath.Resolve(from is null ? null : Path(from), relative).ShouldBe(Path(expected));

    [Theory]
    [InlineData("personal/immich", "../../..")]
    [InlineData("personal/immich", "/etc/passwd")]
    [InlineData(null, "..")]
    public void Resolve_FindsNothingOutOfTheRepository(string? from, string relative) =>
        RepositoryPath.Resolve(from is null ? null : Path(from), relative).ShouldBeNull();

    [Theory]
    [InlineData("personal/immich/compose", "../../../platform/lab/schema/lab.schema.json")]
    [InlineData("platform/lab", "schema/lab.schema.json")]
    [InlineData(null, "platform/lab/schema/lab.schema.json")]
    public void RelativeFrom_IsThePathAFileThereWouldWrite(string? from, string expected)
    {
        var schema = Path("platform/lab/schema/lab.schema.json");
        var directory = from is null ? (RepositoryPath?)null : Path(from);

        schema.RelativeFrom(directory).ShouldBe(expected);
        RepositoryPath.Resolve(directory, expected).ShouldBe(schema);
    }
}
