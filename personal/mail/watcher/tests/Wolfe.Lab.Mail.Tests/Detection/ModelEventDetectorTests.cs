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

    private static readonly TimeZoneInfo London = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    private static IReadOnlyList<DetectedEvent> Read(params Entry[] entries) => ModelEventDetector.Read(new Answer(entries), Received, London);

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
        ModelEventDetector.Read(new Answer([]), Received, London).ShouldBeEmpty();
        ModelEventDetector.Read(new Answer(null), Received, London).ShouldBeEmpty();
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

    [Fact]
    public void Read_UsesTheOffsetOnTheDayNotTheOffsetToday()
    {
        // Booked on 1 October (BST) for a stay after the clocks go back on the 25th. The model
        // wrote today's +01:00 and the invite came out an hour early.
        var found = ReadOne(new Entry("Stay: Malmaison Manchester", "2026-10-31T15:00:00+01:00", "2026-11-04T11:00:00+01:00",
            "Manchester", null, "Europe/London"));

        found.ShouldNotBeNull();
        found.Start.ShouldBe(DateTimeOffset.Parse("2026-10-31T15:00:00+00:00"));
        found.End.ShouldBe(DateTimeOffset.Parse("2026-11-04T11:00:00+00:00"));
    }

    [Fact]
    public void Read_PinsEachEndOfAJourneyToItsOwnZone()
    {
        // New York is still on daylight time on 2 November 2026, a week after London is not.
        var found = ReadOne(new Entry("Flight BA117 London Heathrow to New York JFK", "2026-11-02T09:10:00", "2026-11-02T12:05:00",
            "London Heathrow", null, "Europe/London", "America/New_York"));

        found.ShouldNotBeNull();
        found.Start.ShouldBe(DateTimeOffset.Parse("2026-11-02T09:10:00+00:00"));
        found.End.ShouldBe(DateTimeOffset.Parse("2026-11-02T12:05:00-05:00"));
    }

    [Fact]
    public void Read_EndsInTheStartsZoneWhenOnlyOneIsNamed()
    {
        var found = ReadOne(new Entry("Dinner", "2026-11-02T19:00:00", "2026-11-02T21:00:00", "Paris", null, "Europe/Paris"));

        found.ShouldNotBeNull().End.ShouldBe(DateTimeOffset.Parse("2026-11-02T21:00:00+01:00"));
    }

    [Fact]
    public void Read_ReadsATimeWithNoZoneOrOffsetAtHomeOnThatDay()
    {
        var found = ReadOne(new Entry("Barber", "2026-10-31T14:30:00", null, null, null));

        found.ShouldNotBeNull().Start.ShouldBe(DateTimeOffset.Parse("2026-10-31T14:30:00+00:00"));
    }

    [Fact]
    public void Read_KeepsAnOffsetWhenNoZoneIsNamed()
    {
        // Nothing better to go on, and a foreign offset is more likely right than home's.
        var found = ReadOne(new Entry("Call", "2026-10-31T09:00:00-04:00", null, null, null));

        found.ShouldNotBeNull().Start.ShouldBe(DateTimeOffset.Parse("2026-10-31T09:00:00-04:00"));
    }

    [Fact]
    public void Read_FallsBackToHomeForAZoneThatDoesNotExist()
    {
        var found = ReadOne(new Entry("Barber", "2026-10-31T14:30:00", null, null, null, "Europe/Narnia"));

        found.ShouldNotBeNull().Start.ShouldBe(DateTimeOffset.Parse("2026-10-31T14:30:00+00:00"));
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
