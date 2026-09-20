using Wolfe.Lab.Mail.Detection;

namespace Wolfe.Lab.Mail.Tests.Detection;

public class ModelEventsTests
{
    private static readonly DateTimeOffset Received = DateTimeOffset.Parse("2026-09-20T09:00:00Z");

    private static DetectedEvent? Parse(string answer) => ModelEvents.Parse(answer, Received);

    [Fact]
    public void Parse_ReadsAnEvent()
    {
        var found = Parse("""{"event":true,"summary":"Barber","start":"2026-09-24T14:30:00Z","end":null,"location":"Church St"}""");

        found.ShouldNotBeNull();
        found.Summary.ShouldBe("Barber");
        found.End.ShouldBeNull();
        found.Location.ShouldBe("Church St");
        found.Source.ShouldBe(EventSource.Model);
    }

    [Fact]
    public void Parse_UnwrapsACodeFence()
    {
        // Asked not to, small models fence it anyway.
        Parse("```json\n{\"event\":true,\"summary\":\"Dentist\",\"start\":\"2026-09-25T09:00:00Z\"}\n```")
            .ShouldNotBeNull().Summary.ShouldBe("Dentist");
    }

    [Fact]
    public void Parse_AcceptsTheModelSayingThereIsNoEvent() =>
        Parse("""{"event":false}""").ShouldBeNull();

    [Fact]
    public void Parse_RefusesAnEventWithNoStart() =>
        Parse("""{"event":true,"summary":"Something"}""").ShouldBeNull();

    [Fact]
    public void Parse_RefusesAnEventBeforeTheMailAnnouncingIt() =>
        // The classic hallucination: "Thursday" read as a Thursday last year.
        Parse("""{"event":true,"summary":"Dinner","start":"2025-09-24T19:00:00Z"}""").ShouldBeNull();

    [Fact]
    public void Parse_TreatsRubbishAsNoEvent()
    {
        Parse("I could not find an event in this email.").ShouldBeNull();
        Parse("").ShouldBeNull();
        Parse("{ this is not json }").ShouldBeNull();
    }
}
