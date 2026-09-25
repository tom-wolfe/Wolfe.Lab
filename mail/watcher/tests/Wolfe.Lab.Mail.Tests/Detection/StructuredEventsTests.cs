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

    [Fact]
    public void Read_FindsAFlightByItsRouteAndNumber()
    {
        var found = StructuredEvents.Read(Page("""
            {"@context":"http://schema.org","@type":"FlightReservation","reservationNumber":"RXJ34P",
             "reservationFor":{"@type":"Flight","flightNumber":"117","airline":{"@type":"Airline","name":"British Airways","iataCode":"BA"},
               "departureAirport":{"@type":"Airport","name":"London Heathrow","iataCode":"LHR"},"departureTime":"2026-10-02T09:10:00+01:00",
               "arrivalAirport":{"@type":"Airport","name":"John F. Kennedy International","iataCode":"JFK"},"arrivalTime":"2026-10-02T12:05:00-04:00"}}
            """)).ShouldHaveSingleItem();

        found.Summary.ShouldBe("Flight BA117 LHR → JFK");
        found.Start.ShouldBe(DateTimeOffset.Parse("2026-10-02T09:10:00+01:00"));
        found.End.ShouldBe(DateTimeOffset.Parse("2026-10-02T12:05:00-04:00"));
        found.Location.ShouldBe("LHR");
        found.Reference.ShouldBe("RXJ34P");
        found.AllDay.ShouldBeFalse();
    }

    [Fact]
    public void Read_FindsEveryLegOfAnItinerary()
    {
        var found = StructuredEvents.Read(Page("""
            [{"@context":"http://schema.org","@type":"TrainReservation","reservationNumber":"TL-1",
              "reservationFor":{"@type":"TrainTrip","departureStation":{"@type":"TrainStation","name":"London Euston"},
                "arrivalStation":{"@type":"TrainStation","name":"Manchester Piccadilly"},
                "departureTime":"2026-10-02T08:00:00+01:00","arrivalTime":"2026-10-02T10:10:00+01:00"}},
             {"@context":"http://schema.org","@type":"TrainReservation","reservationNumber":"TL-1",
              "reservationFor":{"@type":"TrainTrip","departureStation":{"@type":"TrainStation","name":"Manchester Piccadilly"},
                "arrivalStation":{"@type":"TrainStation","name":"London Euston"},
                "departureTime":"2026-10-04T17:00:00+01:00","arrivalTime":"2026-10-04T19:10:00+01:00"}}]
            """));

        found.Select(e => e.Summary).ShouldBe(["Train London Euston → Manchester Piccadilly", "Train Manchester Piccadilly → London Euston"]);
    }

    [Fact]
    public void Read_FindsAStayGivenAsDatesAsAllDay()
    {
        var found = StructuredEvents.Read(Page("""
            {"@context":"http://schema.org","@type":"LodgingReservation","reservationNumber":"PI-778",
             "reservationFor":{"@type":"LodgingBusiness","name":"Premier Inn Leeds City Centre",
               "address":{"@type":"PostalAddress","streetAddress":"Whitehall Quay","addressLocality":"Leeds"}},
             "checkinTime":"2026-10-02","checkoutTime":"2026-10-04"}
            """)).ShouldHaveSingleItem();

        found.Summary.ShouldBe("Stay: Premier Inn Leeds City Centre");
        found.Location.ShouldBe("Premier Inn Leeds City Centre, Whitehall Quay, Leeds");
        found.AllDay.ShouldBeTrue();
        found.Reference.ShouldBe("PI-778");
    }

    [Fact]
    public void Read_KeepsAStaysTimesWhenItHasThem() =>
        StructuredEvents.Read(Page("""
            {"@type":"LodgingReservation","reservationFor":{"@type":"Hotel","name":"The Midland"},
             "checkinTime":"2026-10-02T15:00:00+01:00","checkoutTime":"2026-10-04T11:00:00+01:00"}
            """)).ShouldHaveSingleItem().AllDay.ShouldBeFalse();

    [Fact]
    public void Read_FindsATableAndACar()
    {
        var found = StructuredEvents.Read(Page("""
            [{"@type":"FoodEstablishmentReservation","reservationFor":{"@type":"Restaurant","name":"Dishoom"},"startTime":"2026-10-03T19:30:00+01:00"},
             {"@type":"RentalCarReservation","pickupLocation":{"@type":"Place","name":"Hertz Leeds"},
              "pickupTime":"2026-10-02T10:00:00+01:00","dropoffTime":"2026-10-04T10:00:00+01:00"}]
            """));

        found.Select(e => e.Summary).ShouldBe(["Table at Dishoom", "Car hire: Hertz Leeds"], ignoreOrder: true);
    }

    [Fact]
    public void Read_IgnoresAReservationWithoutTheTimesItsTypeNeeds() =>
        StructuredEvents.Read(Page("""
            {"@type":"FlightReservation","reservationFor":{"@type":"Flight","flightNumber":"117",
              "departureAirport":{"iataCode":"LHR"},"arrivalAirport":{"iataCode":"JFK"}}}
            """)).ShouldBeEmpty();

    [Fact]
    public void Read_CountsARepeatedEventOnce() =>
        StructuredEvents.Read(Page("""
            [{"@type":"Event","name":"Hamilton","startDate":"2026-10-02T19:30:00+01:00"},
             {"@type":"EventReservation","reservationFor":{"@type":"Event","name":"Hamilton","startDate":"2026-10-02T19:30:00+01:00"}}]
            """)).ShouldHaveSingleItem();
}
