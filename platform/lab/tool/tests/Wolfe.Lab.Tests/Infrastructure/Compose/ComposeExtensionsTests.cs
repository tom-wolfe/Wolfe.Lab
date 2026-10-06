using Ritten.Docker;
using Wolfe.Lab.Infrastructure.Compose;

namespace Wolfe.Lab.Tests.Infrastructure.Compose;

public class ComposeExtensionsTests
{
    [Fact]
    public void OnLoopback_IsWhereTheHostReachesTheContainersPort()
    {
        var server = Service("server",
            new ComposePort(2283, 2283, null, "tcp"),
            new ComposePort(8081, 9081, "127.0.0.1", "tcp"),
            new ComposePort(8082, 8082, "192.168.1.10", "tcp"),
            new ComposePort(8083, null, null, "tcp"),
            new ComposePort(5353, 5353, null, "udp"));

        server.OnLoopback(2283).ShouldBe(2283);
        server.OnLoopback(8081).ShouldBe(9081);
        server.OnLoopback(8082).ShouldBeNull(); // published on another address
        server.OnLoopback(8083).ShouldBeNull(); // a range, or a port Docker picks
        server.OnLoopback(5353).ShouldBeNull(); // not a scrape's protocol
        server.OnLoopback(9100).ShouldBeNull(); // not published at all
    }

    [Fact]
    public void Container_IsTheNameItGives_OrTheServicesOwn()
    {
        Service("gateway").Container.ShouldBe("gateway");
        (Service("gateway") with { ContainerName = "alloy-gateway" }).Container.ShouldBe("alloy-gateway");
    }

    private static ComposeService Service(string name, params ComposePort[] ports) =>
        new(name, new Dictionary<string, string>(), new Dictionary<string, string?>(), ports);
}
