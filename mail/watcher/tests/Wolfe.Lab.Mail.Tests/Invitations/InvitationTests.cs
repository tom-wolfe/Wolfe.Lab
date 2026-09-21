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

        // Without METHOD:REQUEST a client treats this as a file, and offers nothing.
        ics.ShouldContain("METHOD:REQUEST");
        ics.ShouldContain("BEGIN:VEVENT");
        ics.ShouldContain("SUMMARY:Hamilton");
        ics.ShouldContain("UID:uid-1");
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

        calendar.ContentType.Parameters["method"].ShouldBe("REQUEST");
        calendar.Text.ShouldContain("METHOD:REQUEST");
    }

    [Fact]
    public void Compose_AlsoAttachesTheFile()
    {
        // The shape proven to work by hand, kept alongside the invitation semantics.
        var reply = InvitationSender.Compose(Event(), Source(), "tom@twolfe.dev", Stamp);

        reply.Attachments.OfType<MimePart>().ShouldContain(part => part.FileName == "invite.ics");
    }

    [Fact]
    public void Compose_SaysWhenAModelReadIt()
    {
        var reply = InvitationSender.Compose(Event(EventSource.Model), Source(), "tom@twolfe.dev", Stamp);

        reply.BodyParts.OfType<TextPart>()
            .First(part => part.ContentType.IsMimeType("text", "plain"))
            .Text.ShouldContain("check it before accepting");
    }
}
