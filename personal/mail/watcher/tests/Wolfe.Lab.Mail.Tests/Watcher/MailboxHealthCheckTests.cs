using Microsoft.Extensions.Diagnostics.HealthChecks;
using Wolfe.Lab.Mail.Services.Watcher;

namespace Wolfe.Lab.Mail.Tests.Watcher;

public class MailboxHealthCheckTests
{
    private readonly FixedTime _time = new(DateTimeOffset.Parse("2026-09-22T09:00:00Z"));

    [Fact]
    public async Task Unhealthy_BeforeTheFirstSessionEverConnects()
    {
        // The process is up and serving this endpoint from the moment it starts, so "no answer
        // yet" has to read as unhealthy rather than as healthy-by-default.
        var result = await Check(new MailboxState(_time));

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull().ShouldContain("Never reached the mailbox");
    }

    [Fact]
    public async Task Healthy_WhileTheSessionKeepsTurningOver()
    {
        var state = new MailboxState(_time);
        state.Cycled();

        (await Check(state)).Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Unhealthy_OnceTheSessionHasStoppedTurningOver()
    {
        // The idle loop is capped at 9 minutes, so silence past 15 is not a slow pass.
        var state = new MailboxState(_time);
        state.Cycled();
        _time.Now = _time.Now.AddMinutes(16);

        var result = await Check(state);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull().ShouldContain("No mailbox activity");
    }

    [Fact]
    public async Task Unhealthy_CarriesTheReasonTheSessionEnded()
    {
        // What the endpoint is for: the reader learns why without opening the container's logs.
        var state = new MailboxState(_time);
        state.Lost("SocketException: Name or service not known");

        (await Check(state)).Description.ShouldNotBeNull().ShouldContain("Name or service not known");
    }

    [Fact]
    public async Task Healthy_AgainOnceItReconnects()
    {
        var state = new MailboxState(_time);
        state.Lost("SocketException: Name or service not known");
        state.Cycled();

        var result = await Check(state);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull().ShouldNotContain("Name or service not known");
    }

    private static Task<HealthCheckResult> Check(MailboxState state) =>
        new MailboxHealthCheck(state).CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
