using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Wolfe.Lab.Mail.Models;
using Wolfe.Lab.Mail.Services.Ai;
using Answer = Wolfe.Lab.Mail.Services.Ai.ModelEventDetector.Answer;

namespace Wolfe.Lab.Mail.Tests.Detection;

public class ModelEventDetectorTests
{
    private static readonly DateTimeOffset Received = DateTimeOffset.Parse("2026-09-20T09:00:00Z");

    private static DetectedEvent? Read(Answer answer) => ModelEventDetector.Read(answer, Received);

    [Fact]
    public void Read_TakesTheEventTheModelFound()
    {
        var found = Read(new Answer(true, "Barber", "2026-09-24T14:30:00Z", null, "Church St"));

        found.ShouldNotBeNull();
        found.Summary.ShouldBe("Barber");
        found.End.ShouldBeNull();
        found.Location.ShouldBe("Church St");
        found.Source.ShouldBe(EventSource.Model);
    }

    [Fact]
    public void Read_AcceptsTheModelSayingThereIsNoEvent() =>
        Read(new Answer(false, null, null, null, null)).ShouldBeNull();

    [Fact]
    public void Read_RefusesAnEventWithNoStart() =>
        // The schema settles the shape, so a start can still arrive missing or unreadable.
        Read(new Answer(true, "Something", null, null, null)).ShouldBeNull();

    [Fact]
    public void Read_RefusesAStartItCannotRead() =>
        Read(new Answer(true, "Something", "next Thursday", null, null)).ShouldBeNull();

    [Fact]
    public void Read_RefusesAnEventWithNoSummary() =>
        Read(new Answer(true, "", "2026-09-24T14:30:00Z", null, null)).ShouldBeNull();

    [Fact]
    public void Read_RefusesAnEventBeforeTheMailAnnouncingIt() =>
        // The classic hallucination: "Thursday" read as a Thursday last year.
        Read(new Answer(true, "Dinner", "2025-09-24T19:00:00Z", null, null)).ShouldBeNull();

    [Fact]
    public void Read_DropsAnEndTheModelInventedToFillTheSchema()
    {
        // The schema lets `end` be null and the model usually says so, but not always: the
        // same email has come back both ways. A calendar entry that finishes before it begins
        // is worse than one with no end at all.
        var found = Read(new Answer(true, "Barber", "2026-09-24T14:30:00Z", "2026-09-24T14:30:00Z", null));

        found.ShouldNotBeNull().End.ShouldBeNull();
    }

    [Fact]
    public async Task Read_TurnsTheModelsAnswerIntoAnEvent()
    {
        // The reader over a stubbed client, so the whole path is covered and not just the
        // sense-checking half. It does NOT prove the schema is describable: the extension only
        // builds one for a client that advertises support, and this stub does not.
        var canned = "{\"event\":true,\"summary\":\"Barber\",\"start\":\"2026-09-24T14:30:00Z\"}";
        var reader = new ModelEventDetector(NullLogger<ModelEventDetector>.Instance, new CannedChatClient(canned));

        var found = await reader.Detect("Barber on the 24th at 2:30pm.", Received, TestContext.Current.CancellationToken);

        found.ShouldNotBeNull().Summary.ShouldBe("Barber");
    }

    [Fact]
    public async Task Read_LeavesTheMessageAloneWhenTheModelFails()
    {
        // The model being down must cost nothing: an unread message is seen again next pass,
        // where a throw here would take the watcher's reconnect loop with it.
        var reader = new ModelEventDetector(NullLogger<ModelEventDetector>.Instance, new BrokenChatClient());

        var found = await reader.Detect("Barber on Thursday at 2pm.", Received, TestContext.Current.CancellationToken);

        found.ShouldBeNull();
    }

    private sealed class CannedChatClient(string answer) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class BrokenChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new HttpRequestException("connection refused");

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
