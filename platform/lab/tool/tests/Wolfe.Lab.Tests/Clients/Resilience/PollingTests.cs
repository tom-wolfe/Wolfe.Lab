using Microsoft.Extensions.Time.Testing;
using Wolfe.Lab.Clients.Resilience;

namespace Wolfe.Lab.Tests.Clients.Resilience;

public class PollingTests
{
    private readonly FakeTimeProvider _time = new();

    private Polly.ResiliencePipeline<bool> Pipeline(TimeSpan limit) =>
        Pipelines.Polling("wait", limit, TimeSpan.FromSeconds(1), _time).GetPipeline<bool>("wait");

    /// <summary>Runs the wait, moving the clock a second at a time until it settles.</summary>
    private async Task<bool> Settle(Task<bool> wait)
    {
        while (!wait.IsCompleted)
        {
            await Task.Yield();
            _time.Advance(TimeSpan.FromSeconds(1));
        }

        return await wait;
    }

    [Fact]
    public async Task Until_AsksAgainUntilTheAnswerIsYes()
    {
        var asks = 0;

        var answered = await Settle(Pipeline(TimeSpan.FromSeconds(30)).Until(_ => Task.FromResult(++asks == 3), ct: TestContext.Current.CancellationToken));

        answered.ShouldBeTrue();
        asks.ShouldBe(3);
    }

    [Fact]
    public async Task Until_GivesUpAtTheLimit()
    {
        var asks = 0;

        var answered = await Settle(Pipeline(TimeSpan.FromSeconds(5)).Until(_ => Task.FromResult(++asks < 0), ct: TestContext.Current.CancellationToken));

        answered.ShouldBeFalse();
        asks.ShouldBeInRange(5, 6);
    }

    [Fact]
    public async Task Until_TakesALimitForOneWaitOverThePipelines()
    {
        var asks = 0;

        var answered = await Settle(Pipeline(TimeSpan.FromMinutes(10)).Until(_ => Task.FromResult(++asks < 0), TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));

        answered.ShouldBeFalse();
        asks.ShouldBeInRange(3, 4);
    }
}
