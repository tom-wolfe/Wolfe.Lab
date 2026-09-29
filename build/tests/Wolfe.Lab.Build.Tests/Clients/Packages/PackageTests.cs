using Wolfe.Lab.Build.Clients.Packages;

namespace Wolfe.Lab.Build.Tests.Clients.Packages;

public class PackageTests
{
    private static readonly PackageSettings Tofu = new()
    {
        Github = "opentofu/opentofu",
        Version = "1.12.6",
        Checksums = "tofu_{version}_SHA256SUMS",
        Assets = new Dictionary<string, string>
        {
            ["darwin-arm64"] = "tofu_{version}_darwin_arm64.tar.gz",
            ["linux-arm64"] = "tofu_{version}_linux_arm64.tar.gz"
        }
    };

    [Fact]
    public void From_WritesTheVersionIntoEveryName()
    {
        var package = Package.From("tofu", Tofu, "linux-arm64").Value.ShouldNotBeNull();

        package.ShouldBe(new Package("tofu", "opentofu/opentofu", "1.12.6", "v1.12.6", "tofu_1.12.6_linux_arm64.tar.gz", "tofu_1.12.6_SHA256SUMS"));
        package.Download(package.Asset).ToString()
            .ShouldBe("https://github.com/opentofu/opentofu/releases/download/v1.12.6/tofu_1.12.6_linux_arm64.tar.gz");
    }

    [Fact]
    public void From_TakesASingleAssetOverThePlatforms()
    {
        var settings = Tofu with { Asset = "alloy-darwin-arm64.zip" };

        Package.From("alloy", settings, "linux-arm64").Value.ShouldNotBeNull().Asset.ShouldBe("alloy-darwin-arm64.zip");
    }

    [Fact]
    public void From_RefusesAPlatformWithNoAsset() =>
        Package.From("tofu", Tofu, "windows-amd64").Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("no asset for windows-amd64");

    [Fact]
    public void From_RefusesAPackageWithoutAVersion() =>
        Package.From("tofu", Tofu with { Version = null }, "linux-arm64").Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("'version'");

    [Fact]
    public void From_ReadsATagThatIsNotTheDefault() =>
        Package.From("x", Tofu with { Tag = "release-{version}" }, "linux-arm64").Value.ShouldNotBeNull().Tag.ShouldBe("release-1.12.6");
}
