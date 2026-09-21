using Wolfe.Lab.Mail.Detection;
using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Tests.Detection;

public class StructuredEventsTests
{
    private static string Page(string json) =>
        $"""<html><head><script type="application/ld+json">{json}</script></head><body>hello</body></html>""";

    [Fact]
    public void Read_FindsAPlainEvent()
    {
        var found = StructuredEvents.Read(Page("""
            {"@type":"Event","name":"Hamilton","startDate":"2026-10-02T19:30:00+01:00",
             "endDate":"2026-10-02T22:00:00+01:00","location":{"@type":"Place","name":"Victoria Palace"}}
            """)).ShouldHaveSingleItem();

        found.Summary.ShouldBe("Hamilton");
        found.Start.ShouldBe(DateTimeOffset.Parse("2026-10-02T19:30:00+01:00"));
        found.End.ShouldBe(DateTimeOffset.Parse("2026-10-02T22:00:00+01:00"));
        found.Location.ShouldBe("Victoria Palace");
        found.Source.ShouldBe(EventSource.StructuredData);
    }

    [Fact]
    public void Read_ReachesInsideAReservation()
    {
        // The shape a ticket actually arrives in: the event is nested under reservationFor.
        var found = StructuredEvents.Read(Page("""
            {"@type":"EventReservation","reservationNumber":"X1",
             "reservationFor":{"@type":"Event","name":"Cup Final","startDate":"2026-05-30T15:00:00Z"}}
            """)).ShouldHaveSingleItem();

        found.Summary.ShouldBe("Cup Final");
    }

    [Fact]
    public void Read_PrefersTheThingCallingItselfAnEvent()
    {
        // Some senders put a date on the wrapper too; the Event should win.
        var found = StructuredEvents.Read(Page("""
            [{"@type":"Thing","name":"Booking confirmation","startDate":"2026-01-01T09:00:00Z"},
             {"@type":"Event","name":"Barber","startDate":"2026-01-02T10:00:00Z"}]
            """));

        found.First().Summary.ShouldBe("Barber");
    }

    [Fact]
    public void Read_TakesAnAddressWhenThereIsNoPlaceName()
    {
        StructuredEvents.Read(Page("""
            {"@type":"Event","name":"Dentist","startDate":"2026-03-04T09:00:00Z",
             "location":{"@type":"Place","address":{"@type":"PostalAddress","streetAddress":"12 High St"}}}
            """)).ShouldHaveSingleItem().Location.ShouldBe("12 High St");
    }

    [Fact]
    public void Read_IgnoresStructuredDataThatIsNotAnEvent()
    {
        StructuredEvents.Read(Page("""{"@type":"Organization","name":"Trainline"}""")).ShouldBeEmpty();
    }

    [Fact]
    public void Read_SurvivesOneMalformedBlock()
    {
        var html = Page("{not json") + Page("""{"@type":"Event","name":"Standup","startDate":"2026-02-02T09:00:00Z"}""");

        StructuredEvents.Read(html).ShouldHaveSingleItem().Summary.ShouldBe("Standup");
    }

    [Fact]
    public void Read_HasNothingToSayAboutAMessageWithNoHtml() => StructuredEvents.Read(null).ShouldBeEmpty();
}
