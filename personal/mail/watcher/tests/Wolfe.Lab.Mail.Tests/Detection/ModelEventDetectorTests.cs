using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Wolfe.Lab.Mail.Models;
using Wolfe.Lab.Mail.Services.Ai;
using Wolfe.Lab.Mail.Tests.Fakes;
using Answer = Wolfe.Lab.Mail.Services.Ai.ModelEventDetector.Answer;
using Entry = Wolfe.Lab.Mail.Services.Ai.ModelEventDetector.Entry;

namespace Wolfe.Lab.Mail.Tests.Detection;

public class ModelEventDetectorTests
{
    private static readonly DateTimeOffset Received = DateTimeOffset.Parse("2026-09-20T09:00:00Z");

    private static IReadOnlyList<DetectedEvent> Read(params Entry[] entries) => ModelEventDetector.Read(new Answer(entries), Received);

    private static DetectedEvent? ReadOne(Entry entry) => Read(entry).SingleOrDefault();

    [Fact]
    public void Read_TakesTheEventTheModelFound()
    {
        var found = ReadOne(new Entry("Barber", "2026-09-24T14:30:00Z", null, "Church St", null));

        found.ShouldNotBeNull();
        found.Summary.ShouldBe("Barber");
        found.End.ShouldBeNull();
        found.Location.ShouldBe("Church St");
        found.Source.ShouldBe(EventSource.Model);
        found.AllDay.ShouldBeFalse();
    }

    [Fact]
    public void Read_AcceptsTheModelSayingThereIsNoEvent()
    {
        ModelEventDetector.Read(new Answer([]), Received).ShouldBeEmpty();
        ModelEventDetector.Read(new Answer(null), Received).ShouldBeEmpty();
    }

    [Fact]
    public void Read_TakesEveryLegOfAJourney()
    {
        var found = Read(
            new Entry("Train London Euston to Manchester Piccadilly", "2026-10-02T08:00:00+01:00", "2026-10-02T10:10:00+01:00", "London Euston", "ABC123"),
            new Entry("Train Manchester Piccadilly to London Euston", "2026-10-04T17:00:00+01:00", "2026-10-04T19:10:00+01:00", "Manchester Piccadilly", "ABC123"));

        found.Select(e => e.Location).ShouldBe(["London Euston", "Manchester Piccadilly"]);
        found.ShouldAllBe(e => e.Reference == "ABC123");
    }

    [Fact]
    public void Read_TakesAStayGivenOnlyAsDatesAsAllDay()
    {
        var found = ReadOne(new Entry("Stay: Premier Inn Leeds City Centre", "2026-10-02", "2026-10-04", "Leeds", "PI-778"));

        found.ShouldNotBeNull().AllDay.ShouldBeTrue();
        found.Start.Date.ShouldBe(new DateTime(2026, 10, 2));
        found.End.ShouldNotBeNull().Date.ShouldBe(new DateTime(2026, 10, 4));
        found.Reference.ShouldBe("PI-778");
    }

    [Fact]
    public void Read_DropsTheNonsenseAndKeepsTheRest()
    {
        var found = Read(
            new Entry("Something", "next Thursday", null, null, null),
            new Entry("Flight BA117 LHR to JFK", "2026-10-02T09:10:00+01:00", null, "LHR", null));

        found.ShouldHaveSingleItem().Summary.ShouldBe("Flight BA117 LHR to JFK");
    }

    [Fact]
    public void Read_RefusesAnEventWithNoStart() =>
        // The schema settles the shape, so a start can still arrive missing or unreadable.
        ReadOne(new Entry("Something", null, null, null, null)).ShouldBeNull();

    [Fact]
    public void Read_RefusesAStartItCannotRead() =>
        ReadOne(new Entry("Something", "next Thursday", null, null, null)).ShouldBeNull();

    [Fact]
    public void Read_RefusesAnEventWithNoSummary() =>
        ReadOne(new Entry("", "2026-09-24T14:30:00Z", null, null, null)).ShouldBeNull();

    [Fact]
    public void Read_RefusesAnEventBeforeTheMailAnnouncingIt() =>
        // The classic hallucination: "Thursday" read as a Thursday last year.
        ReadOne(new Entry("Dinner", "2025-09-24T19:00:00Z", null, null, null)).ShouldBeNull();

    [Fact]
    public void Read_DropsAnEndTheModelInventedToFillTheSchema()
    {
        // The schema lets `end` be null and the model usually says so, but not always: the
        // same email has come back both ways. A calendar entry that finishes before it begins
        // is worse than one with no end at all.
        var found = ReadOne(new Entry("Barber", "2026-09-24T14:30:00Z", "2026-09-24T14:30:00Z", null, null));

        found.ShouldNotBeNull().End.ShouldBeNull();
    }

    [Theory]
    [InlineData("2026-10-02", true)]
    [InlineData("2026-10-02T15:00:00+01:00", false)]
    [InlineData("2026-10-02 15:00", false)]
    public void IsDateOnly_TellsADayFromAnInstant(string value, bool dateOnly) =>
        ModelEventDetector.IsDateOnly(value).ShouldBe(dateOnly);

    [Fact]
    public async Task Read_TurnsTheModelsAnswerIntoAnEvent()
    {
        // The reader over a stubbed client, so the whole path is covered and not just the
        // sense-checking half. It does NOT prove the schema is describable: the extension only
        // builds one for a client that advertises support, and this stub does not.
        var canned = "{\"entries\":[{\"summary\":\"Barber\",\"start\":\"2026-09-24T14:30:00Z\"}]}";
        var reader = Detector(new CannedChatClient(canned));

        var found = await reader.Detect("Barber on the 24th at 2:30pm.", Received, TestContext.Current.CancellationToken);

        found.ShouldHaveSingleItem().Summary.ShouldBe("Barber");
    }

    [Fact]
    public async Task Detect_LetsAnUnavailableModelFailSoTheEmailIsReadAgain()
    {
        // Not "no event": the watcher must stop short of this email and ask again, or it is
        // never read by a model at all.
        var reader = Detector(new BrokenChatClient(Endpoint.Down()));

        await Should.ThrowAsync<HttpRequestException>(() =>
            reader.Detect("Barber on Thursday at 2pm.", Received, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Detect_TreatsAnAnswerOutsideTheSchemaAsNoEvent()
    {
        // The model was asked; retrying one email that confuses it would stop the whole inbox.
        var reader = Detector(new CannedChatClient("I think there might be a haircut?"));

        var found = await reader.Detect("Barber on Thursday at 2pm.", Received, TestContext.Current.CancellationToken);

        found.ShouldBeEmpty();
    }

    private static ModelEventDetector Detector(IChatClient chat) => new(NullLogger<ModelEventDetector>.Instance, chat);
}
