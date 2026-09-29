using HtmlAgilityPack;
using MimeKit;

namespace Wolfe.Lab.Mail.Mailbox;

/// <summary>
/// The readable text of a message, for the tier that reads prose.
/// </summary>
internal static class MessageText
{
    /// <summary>
    /// What the message says, or null when it says nothing readable.
    /// </summary>
    /// <remarks>
    /// The plain-text part when the sender provided one, and otherwise the HTML with its markup
    /// taken off. Handing the HTML over raw is the thing this exists to stop: a marketing email
    /// is mostly stylesheet, so the first several thousand characters are <c>@font-face</c>
    /// rules and the message never reaches the part that says what was booked.
    /// </remarks>
    public static string? Prose(MimeMessage message) => message switch
    {
        { TextBody: { Length: > 0 } text } => text,
        { HtmlBody: { Length: > 0 } html } => FromHtml(html),
        _ => null
    };

    /// <summary>
    /// The text of an HTML body, with everything that is not text removed.
    /// </summary>
    internal static string FromHtml(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        // Script, style and head carry no message, and a stylesheet is most of the file. Their
        // text would otherwise come through as text, because InnerText does not know the
        // difference. SelectNodes answers null rather than empty when nothing matches, so a
        // body with none of these must not be a crash.
        foreach (var node in document.DocumentNode.SelectNodes("//script|//style|//head")?.ToList() ?? [])
        {
            node.Remove();
        }

        var text = HtmlEntity.DeEntitize(document.DocumentNode.InnerText) ?? string.Empty;

        // Layout leaves runs of whitespace behind wherever the markup was; collapsing them is
        // what makes the length honest about how much the message actually says.
        return string.Join('\n', text
            .Split('\n', StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 0)
            .Select(line => string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))));
    }
}
