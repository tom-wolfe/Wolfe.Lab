using Wolfe.Lab.Infrastructure.Compose;

namespace Wolfe.Lab.Tests.Infrastructure.Compose;

public class ComposeProjectTests
{
    [Fact]
    public void Parse_ReadsEachServicesLabelsAndEnvironment()
    {
        var project = ComposeProject.Parse("""
            { "services": {
                "watcher": { "labels": { "lab.logs": "otlp" }, "environment": { "TZ": "Europe/London", "TOKEN": null } },
                "bridge": { "image": "busybox" } } }
            """).Value.ShouldNotBeNull();

        project.Services.Select(service => service.Name).ShouldBe(["bridge", "watcher"]);
        project.Services[0].Labels.ShouldBeEmpty();
        project.Services[1].Labels["lab.logs"].ShouldBe("otlp");
        project.Services[1].Environment["TOKEN"].ShouldBeNull();
    }

    [Fact]
    public void Parse_ReadsThePortsEachPublishesAndWhoseNetworkItRunsIn()
    {
        var project = ComposeProject.Parse("""
            { "services": {
                "server": { "ports": [
                    { "target": 2283, "published": "2283", "protocol": "tcp" },
                    { "target": 8081, "published": "9081", "host_ip": "127.0.0.1", "protocol": "tcp" },
                    { "target": 8082, "published": "8082", "host_ip": "192.168.1.10", "protocol": "tcp" },
                    { "target": 8083, "published": "8083-8084", "protocol": "tcp" },
                    { "target": 9100, "protocol": "tcp" } ] },
                "sidecar": { "network_mode": "service:server" } } }
            """).Value.ShouldNotBeNull();

        var server = project.Services[0];
        server.OnLoopback(2283).ShouldBe(2283);
        server.OnLoopback(8081).ShouldBe(9081);
        server.OnLoopback(8082).ShouldBeNull(); // published on another address
        server.OnLoopback(8083).ShouldBeNull(); // a range, so no one port
        server.OnLoopback(9100).ShouldBeNull(); // on a port Docker picks
        server.NetworkMode.ShouldBeNull();
        project.Services[1].NetworkMode.ShouldBe("service:server");
    }

    [Fact]
    public void Parse_RefusesWhatIsNotComposesConfiguration() =>
        ComposeProject.Parse("not json").IsError.ShouldBeTrue();
}

// Against the real compose: that it reads both of a file's spellings as one is what the lab relies on.
