using System.Text.Json;
using Wolfe.Lab.Domain.Catalog;

namespace Wolfe.Lab.Tests.Domain.Catalog;

public class ClosedSetTests
{
    [Fact]
    public void AValueIsItsOwnSpelling()
    {
        ComponentKind.Infrastructure.Value.ShouldBe("infrastructure");
        Lifecycle.Production.ToString().ShouldBe("production");
        LinkType.Runbook.Value.ShouldBe("runbook");
    }

    [Fact]
    public void All_ListsEveryValueInItsOrder() =>
        Lifecycle.All.Select(lifecycle => lifecycle.Value).ShouldBe(["production", "experimental", "deprecated"]);

    [Fact]
    public void From_TakesOnlyAValueOfTheSet()
    {
        ComponentKind.From("workload").ShouldBe(ComponentKind.Workload);
        ComponentKind.TryFrom("daemon").Error.ErrorMessage
            .ShouldBe("'daemon' is not a kind of component (workload, backup, runner, repository, infrastructure, certificate, package, machine).");
    }

    private sealed record Holder(LinkType Type);

    [Fact]
    public void AValueIsWrittenAndReadAsItsSpelling()
    {
        var json = JsonSerializer.Serialize(new Holder(LinkType.Dashboard));

        json.ShouldBe("""{"Type":"dashboard"}""");
        JsonSerializer.Deserialize<Holder>(json).ShouldNotBeNull().Type.ShouldBe(LinkType.Dashboard);
    }
}
