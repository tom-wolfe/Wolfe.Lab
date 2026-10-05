using Ritten.Engine.FileSystem;
using Wolfe.Lab.Infrastructure.Logrotate;

namespace Wolfe.Lab.Tests.Infrastructure.Logrotate;

public class LogrotateConfigurationTests
{
    [Fact]
    public void For_KeepsEachLogToItsSizeAndGenerations_WithoutMovingItFromUnderItsAgent() =>
        LogrotateConfiguration.For([new PhysicalFile("/lab/root/logs/monitoring-alloy-forwarder.log")]).ShouldBe("""
            "/lab/root/logs/monitoring-alloy-forwarder.log" {
                size 10M
                rotate 5
                copytruncate
                compress
                delaycompress
                missingok
                notifempty
            }

            """.ReplaceLineEndings());

    [Fact]
    public void For_WritesABlockPerLog() =>
        LogrotateConfiguration.For([new PhysicalFile("/a.log"), new PhysicalFile("/b.log")]).Split('{').Length.ShouldBe(3);
}
