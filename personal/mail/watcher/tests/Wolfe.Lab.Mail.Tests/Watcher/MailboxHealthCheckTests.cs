using System.Diagnostics;
using System.Net.Sockets;
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
        state.Lost(NotKnown, default);

        (await Check(state)).Description.ShouldNotBeNull().ShouldContain("Name or service not known");
    }

    [Fact]
    public async Task Unhealthy_CarriesTheExceptionItself()
    {
        // The failed check's log is written from the result, so without the exception it has the
        // message and no stack trace.
        var state = new MailboxState(_time);
        state.Lost(NotKnown, default);

        (await Check(state)).Exception.ShouldBeSameAs(NotKnown);
    }

    [Fact]
    public async Task Unhealthy_LeadsToTheTraceTheSessionEndedIn()
    {
        using var source = new ActivitySource(nameof(Unhealthy_LeadsToTheTraceTheSessionEndedIn));
        using var listener = new ActivityListener
        {
            ShouldListenTo = candidate => candidate == source,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var ended = new ActivityContext(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        var state = new MailboxState(_time);
        state.Lost(NotKnown, ended);

        using var probe = source.StartActivity("probe").ShouldNotBeNull();
        var result = await Check(state);

        result.Description.ShouldNotBeNull().ShouldContain(ended.TraceId.ToString());
        probe.Links.ShouldContain(link => link.Context == ended);
    }

    [Fact]
    public async Task Healthy_AgainOnceItReconnects()
    {
        var state = new MailboxState(_time);
        state.Lost(NotKnown, default);
        state.Cycled();

        var result = await Check(state);

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull().ShouldNotContain("Name or service not known");
    }

    private static readonly SocketException NotKnown = new((int)SocketError.HostNotFound, "Name or service not known");

    private static Task<HealthCheckResult> Check(MailboxState state) =>
        new MailboxHealthCheck(state).CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
