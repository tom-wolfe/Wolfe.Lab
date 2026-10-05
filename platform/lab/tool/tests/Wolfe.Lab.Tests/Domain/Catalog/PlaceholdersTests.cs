using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Tests.Domain.Catalog;

public class PlaceholdersTests
{
    [Fact]
    public void In_FindsEachDottedLowerName() =>
        Placeholders.In("{package}/alloy-{platform} {node.mini.address} ${LAB_ROOT} {Upper}").ShouldBe(["package", "platform", "node.mini.address"]);

    [Fact]
    public void Expand_LeavesWhatItHasNoValueForAsWritten() =>
        Placeholders.Expand("{package}/alloy-{platform}", name => name == "platform" ? "linux-arm64" : null).ShouldBe("{package}/alloy-linux-arm64");
}
