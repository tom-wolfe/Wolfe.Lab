using Microsoft.Extensions.Configuration;
using Wolfe.Lab.Domain.Catalog.Nodes;
using Wolfe.Lab.Infrastructure.Releases;

namespace Wolfe.Lab.Tests.Infrastructure.Releases;

public class LabNodeTests
{
    // As the lab's configuration has it: the environment's LAB_ prefix stripped.
    private static LabNode Configured(params (string Key, string Value)[] settings)
    {
        var node = new LabNode();
        node.Configure(new ConfigurationBuilder().AddInMemoryCollection(settings.Select(setting => KeyValuePair.Create(setting.Key, (string?)setting.Value))).Build());
        return node;
    }

    [Fact]
    public void Configure_TakesTheNodeFromTheConfiguration() =>
        Configured(("NODE", "mini")).Name.ShouldBe(NodeName.From("mini"));

    [Fact]
    public void Configure_LeavesItNoNode_WhenUnsetOrEmpty()
    {
        Configured().Name.ShouldBeNull();
        Configured(("NODE", "")).Given.ShouldBeNull();
    }

    [Fact]
    public void Name_IsNone_WhenWhatIsGivenIsNotAName()
    {
        var node = Configured(("NODE", "MacMini"));

        node.Given.ShouldBe("MacMini");
        node.Name.ShouldBeNull();
    }
}
