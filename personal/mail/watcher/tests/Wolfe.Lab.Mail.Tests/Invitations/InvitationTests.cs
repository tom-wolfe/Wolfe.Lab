using MimeKit;
using Wolfe.Lab.Mail.Invitations;
using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Tests.Invitations;

public class InvitationTests
{
    private static readonly DateTimeOffset Stamp = DateTimeOffset.Parse("2026-09-20T09:00:00Z");

    private static DetectedEvent Event(EventSource source = EventSource.StructuredData) => new(
        "Hamilton",
        DateTimeOffset.Parse("2026-10-02T19:30:00Z"),
        DateTimeOffset.Parse("2026-10-02T22:00:00Z"),
        "Victoria Palace",
        source);

    private static MimeMessage Source()
    {
        var message = new MimeMessage { Subject = "Your tickets", MessageId = "abc123@ticketmaster.com" };
        message.From.Add(MailboxAddress.Parse("noreply@ticketmaster.com"));
        message.To.Add(MailboxAddress.Parse("tom@twolfe.dev"));
        message.References.Add("earlier@ticketmaster.com");
        message.Body = new TextPart("plain") { Text = "Here are your tickets." };
        return message;
    }

    [Fact]
    public void Build_DeclaresTheMethodThatMakesItAnInvitation()
    {
        var ics = InvitationBuilder.Build(Event(), "tom@twolfe.dev", "uid-1", Stamp);

        // Without a METHOD a client treats this as a file, and offers nothing. PUBLISH rather
        // than REQUEST: there is nobody to invite, and a REQUEST the recipient organises is one
        // a client declines to offer.
        ics.ShouldContain("METHOD:PUBLISH");
        ics.ShouldContain("BEGIN:VEVENT");
        ics.ShouldContain("SUMMARY:Hamilton");
        ics.ShouldContain("UID:uid-1");
        // The pair that made a client treat it as its own event and offer nothing.
        ics.ShouldNotContain("ATTENDEE");
    }

    [Fact]
    public void Build_GivesAnEventWithNoStatedEndAnHour()
    {
        var open = Event() with { End = null };

        var ics = InvitationBuilder.Build(open, "tom@twolfe.dev", "uid-2", Stamp);

        ics.ShouldContain("DTSTART:20261002T193000Z");
        ics.ShouldContain("DTEND:20261002T203000Z");
    }

    [Fact]
    public void UidFor_IsStableForTheSameMessage() =>
        InvitationBuilder.UidFor("<abc@x>").ShouldBe(InvitationBuilder.UidFor("abc@x"));

    [Fact]
    public void Compose_RepliesIntoTheSameConversation()
    {
        var reply = InvitationSender.Compose(Event(), Source(), "tom@twolfe.dev", Stamp);

        reply.InReplyTo.ShouldBe("abc123@ticketmaster.com");
        reply.References.ShouldContain("earlier@ticketmaster.com");
        reply.References.ShouldContain("abc123@ticketmaster.com");
        reply.Subject.ShouldBe("Re: Your tickets");
    }

    [Fact]
    public void Compose_NeverAddressesAnyoneButTheConfiguredMailbox()
    {
        // The whole danger of a reply-shaped mail: it must not answer the sender.
        var reply = InvitationSender.Compose(Event(), Source(), "tom@twolfe.dev", Stamp);

        reply.To.Mailboxes.Select(m => m.Address).ShouldBe(["tom@twolfe.dev"]);
        reply.Cc.ShouldBeEmpty();
        reply.Bcc.ShouldBeEmpty();
        reply.From.Mailboxes.Select(m => m.Address).ShouldBe(["tom@twolfe.dev"]);
    }

    [Fact]
    public void Compose_DoesNotStackReOnAReply()
    {
        var source = Source();
        source.Subject = "Re: Your tickets";

        InvitationSender.Compose(Event(), source, "tom@twolfe.dev", Stamp).Subject.ShouldBe("Re: Your tickets");
    }

    [Fact]
    public void Compose_CarriesTheCalendarPartWithItsMethod()
    {
        var reply = InvitationSender.Compose(Event(), Source(), "tom@twolfe.dev", Stamp);

        var calendar = reply.BodyParts.OfType<TextPart>()
            .Single(part => part.ContentType.IsMimeType("text", "calendar"));

        calendar.ContentType.Parameters["method"].ShouldBe("PUBLISH");
        calendar.Text.ShouldContain("METHOD:PUBLISH");
    }

    [Fact]
    public void Compose_SendsTheCalendarExactlyOnce()
    {
        // It used to send the same bytes twice — once as the invitation and once as an
        // application/ics attachment — which arrived as two .ics files and left the client
        // interpreting neither.
        var reply = InvitationSender.Compose(Event(), Source(), "tom@twolfe.dev", Stamp);

        reply.BodyParts.Count(part => part.ContentType.IsMimeType("text", "calendar")
                                      || part.ContentType.IsMimeType("application", "ics"))
            .ShouldBe(1);
        reply.Attachments.ShouldBeEmpty();
    }

    [Fact]
    public void Compose_SaysWhenAModelReadIt()
    {
        var reply = InvitationSender.Compose(Event(EventSource.Model), Source(), "tom@twolfe.dev", Stamp);

        reply.BodyParts.OfType<TextPart>()
            .First(part => part.ContentType.IsMimeType("text", "plain"))
            .Text.ShouldContain("check it before accepting");
    }

    [Fact]
    public void Build_WritesAStayAsDaysIncludingCheckOutDay()
    {
        var stay = new DetectedEvent("Stay: Premier Inn Leeds", DateTimeOffset.Parse("2026-10-02T00:00:00+01:00"),
            DateTimeOffset.Parse("2026-10-04T00:00:00+01:00"), "Leeds", EventSource.StructuredData, AllDay: true);

        var ics = InvitationBuilder.Build(stay, "tom@twolfe.dev", "uid-3", Stamp);

        // DTEND is exclusive: the day after check-out, so check-out day is on the calendar too.
        ics.ShouldContain("DTSTART;VALUE=DATE:20261002");
        ics.ShouldContain("DTEND;VALUE=DATE:20261005");
    }

    [Fact]
    public void Build_MakesASingleDateADay()
    {
        var day = new DetectedEvent("Stay: somewhere", DateTimeOffset.Parse("2026-10-02T00:00:00+01:00"), null, null,
            EventSource.Model, AllDay: true);

        var ics = InvitationBuilder.Build(day, "tom@twolfe.dev", "uid-4", Stamp);

        ics.ShouldContain("DTSTART;VALUE=DATE:20261002");
        ics.ShouldContain("DTEND;VALUE=DATE:20261003");
    }

    [Fact]
    public void Build_CarriesTheBookingReference() =>
        InvitationBuilder.Build(Event() with { Reference = "RXJ34P" }, "tom@twolfe.dev", "uid-5", Stamp)
            .ShouldContain("DESCRIPTION:Booking reference: RXJ34P");

    [Fact]
    public void UidFor_GivesEachEventInAMessageItsOwn()
    {
        // The first keeps the UID a message's only event always had, so nothing already sent
        // is duplicated; the rest are distinct, so a return leg does not overwrite the outbound.
        InvitationBuilder.UidFor("<abc@x>", 0).ShouldBe(InvitationBuilder.UidFor("<abc@x>"));
        InvitationBuilder.UidFor("<abc@x>", 1).ShouldNotBe(InvitationBuilder.UidFor("<abc@x>", 0));
        InvitationBuilder.UidFor("<abc@x>", 1).ShouldBe(InvitationBuilder.UidFor("abc@x", 1));
    }

    [Fact]
    public void Compose_SaysTheReferenceInThePlainText()
    {
        var reply = InvitationSender.Compose(Event() with { Reference = "RXJ34P" }, Source(), "tom@twolfe.dev", Stamp);

        reply.BodyParts.OfType<TextPart>()
            .First(part => part.ContentType.IsMimeType("text", "plain"))
            .Text.ShouldContain("Reference: RXJ34P");
    }
}
