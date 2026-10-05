using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Tests.Domain.Catalog;

public class TemplateTests
{
    [Fact]
    public void Placeholders_AreEachDottedLowerName() =>
        Template.From("{package}/alloy-{platform} {node.mini.address} ${LAB_ROOT} {Upper}").Placeholders.ShouldBe(["package", "platform", "node.mini.address"]);

    [Fact]
    public void Expand_LeavesWhatItHasNoValueForAsWritten() =>
        Template.From("{package}/alloy-{platform}").Expand(name => name == "platform" ? "linux-arm64" : null)
            .ShouldBe(Template.From("{package}/alloy-linux-arm64"));

    [Fact]
    public void Expand_LeavesOnlyWhatIsStillToBeWrittenIn() =>
        Template.From("{package}/alloy-{platform}").Expand(name => name == "platform" ? "linux-arm64" : null).Placeholders.ShouldBe(["package"]);
}
