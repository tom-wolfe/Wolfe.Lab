using MimeKit;
using Wolfe.Lab.Mail.Mailbox;

namespace Wolfe.Lab.Mail.Tests.Mailbox;

public class MessageTextTests
{
    private static string Ticketmaster =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ticketmaster.html"));

    [Fact]
    public void FromHtml_LeavesOnlyWhatTheMessageSays()
    {
        // The whole point, in one number: a real marketing email is mostly stylesheet, and the
        // detector only ever sees the first few thousand characters of what it is handed.
        var text = MessageText.FromHtml(Ticketmaster);

        text.Length.ShouldBeLessThan(Ticketmaster.Length / 10);
        text.ShouldNotContain("@font-face");
        text.ShouldNotContain("<table");
    }

    [Fact]
    public void FromHtml_PutsTheEventWhereTheDetectorWillSeeIt()
    {
        // Untouched, this email carries its date past offset 21,000 — well beyond the trim.
        var text = MessageText.FromHtml(Ticketmaster);

        text.IndexOf("Sat 24 Oct 2026", StringComparison.Ordinal).ShouldBeLessThan(1000);
        text.ShouldContain("THE DEVIL WEARS PRADA");
        text.ShouldContain("New Century Hall");
    }

    [Fact]
    public void FromHtml_SurvivesABodyWithNothingToStrip() =>
        // SelectNodes answers null rather than empty, so this is the crash that would happen on
        // the plainest email in the mailbox.
        MessageText.FromHtml("<p>Barber on Thursday at 2pm.</p>").ShouldBe("Barber on Thursday at 2pm.");

    [Fact]
    public void Prose_PrefersThePlainTextPartTheSenderWrote()
    {
        var message = new MimeMessage
        {
            Body = new Multipart("alternative")
            {
                new TextPart("plain") { Text = "Barber on Thursday." },
                new TextPart("html") { Text = "<html><body><p>Something else entirely.</p></body></html>" }
            }
        };

        MessageText.Prose(message).ShouldBe("Barber on Thursday.");
    }

    [Fact]
    public void Prose_FallsBackToTheHtmlWithItsMarkupOff()
    {
        var message = new MimeMessage { Body = new TextPart("html") { Text = Ticketmaster } };

        var prose = MessageText.Prose(message).ShouldNotBeNull();
        prose.ShouldContain("THE DEVIL WEARS PRADA");
        prose.ShouldNotContain("@font-face");
    }

    [Fact]
    public void Prose_SaysNothingWhenTheMessageHasNoBody() =>
        MessageText.Prose(new MimeMessage()).ShouldBeNull();
}
