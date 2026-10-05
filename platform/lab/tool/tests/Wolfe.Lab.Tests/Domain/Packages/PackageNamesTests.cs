using Wolfe.Lab.Domain.Packages;

namespace Wolfe.Lab.Tests.Domain.Packages;

public class PackageNamesTests
{
    [Fact]
    public void GitHubRepository_IsAnOwnerAndAName()
    {
        var repository = GitHubRepository.From("grafana/alloy");

        repository.Owner.ShouldBe("grafana");
        repository.Name.ShouldBe("alloy");
    }

    [Theory]
    [InlineData("alloy")]
    [InlineData("grafana/alloy/extra")]
    [InlineData("grafana/")]
    [InlineData("../alloy")]
    [InlineData("grafana/al loy")]
    public void GitHubRepository_RefusesWhatIsNotOne(string given) =>
        GitHubRepository.TryFrom(given).Error.ErrorMessage.ShouldContain("is not a GitHub repository");

    [Theory]
    [InlineData("1.20.1")]
    [InlineData("0.21.0-rc.1")]
    [InlineData("2026.10.05+build")]
    public void PackageVersion_TakesWhatAReleaseIsNamed(string given) =>
        PackageVersion.TryFrom(given).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("../1.20.1")]
    [InlineData("1.20/1")]
    [InlineData(".hidden")]
    [InlineData("1.20 1")]
    public void PackageVersion_RefusesWhatCouldNotNameItsDirectory(string given) =>
        PackageVersion.TryFrom(given).Error.ErrorMessage.ShouldContain("is not a version");
}
