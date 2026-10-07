using Wolfe.Lab.Domain.Catalog.Components;
using Wolfe.Lab.Domain.Catalog.Components.Compose;

namespace Wolfe.Lab.Tests.Domain.Catalog.Components.Compose;

public class DotNetServiceComponentTests
{
    [Fact]
    public void Image_IsNamedForItsServiceAndItself() =>
        Catalogs.Component("personal/mail/watcher", Catalogs.Docker("watcher", "watcher") with { Workflow = WorkflowName.DotNetService })
            .ShouldBeOfType<DotNetServiceComponent>().Image.ShouldBe("lab/mail-watcher");

    [Fact]
    public void ADockerComponentIsNoneOfThem() =>
        Catalogs.Component("personal/mail/bridge", "bridge").ShouldBeOfType<DockerComponent>().Workflow.ShouldBe(WorkflowName.Docker);
}
