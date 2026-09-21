using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using MimeKit;
using Wolfe.Lab.Mail.Services.Watcher;

using Microsoft.Extensions.Options;
using Wolfe.Lab.Mail.Models;

namespace Wolfe.Lab.Mail.Invitations;

/// <summary>
/// Mails the invitation back to the mailbox it came from, in the same conversation.
/// </summary>
internal sealed class InvitationSender(IOptions<MailboxWatcherOptions> options, ILogger<InvitationSender> log)
{
    public async Task Send(DetectedEvent detected, MimeMessage source, CancellationToken ct = default)
    {
        var message = Compose(detected, source, options.Value.Recipient, DateTimeOffset.UtcNow);

        using var client = new SmtpClient();
        // Bridge presents its own self-signed certificate on a port only reachable from this
        // Docker network. Verifying it would mean pinning a certificate the bridge regenerates.
        client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        client.CheckCertificateRevocation = false;

        await client.ConnectAsync(options.Value.BridgeHost, options.Value.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(options.Value.Username, options.Value.Password, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        log.LogInformation("Invited {Summary} at {Start:u} in reply to {Subject}", detected.Summary, detected.Start, source.Subject);
    }

    /// <summary>
    /// The message itself, kept pure so the threading and the calendar part can be asserted
    /// without a server.
    /// </summary>
    internal static MimeMessage Compose(DetectedEvent detected, MimeMessage source, string recipient, DateTimeOffset stamp)
    {
        var message = new MimeMessage();
        var self = MailboxAddress.Parse(recipient);
        message.From.Add(self);

        // The ONLY recipient, and it comes from configuration. Nothing is read off the source
        // message's From, To or Reply-To: a reply-shaped mail that took its address from the
        // thread would answer the airline.
        message.To.Add(self);

        var subject = source.Subject is { Length: > 0 } original ? original : detected.Summary;
        message.Subject = subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase) ? subject : $"Re: {subject}";

        // What puts it in the conversation. Proton threads on this chain, so the invitation
        // lands under the booking rather than as a message about nothing.
        if (source.MessageId is { Length: > 0 } messageId)
        {
            message.InReplyTo = messageId;
            foreach (var reference in source.References)
            {
                message.References.Add(reference);
            }

            message.References.Add(messageId);
        }

        var calendar = InvitationBuilder.Build(
            detected,
            self.Address,
            InvitationBuilder.UidFor(source.MessageId ?? Guid.NewGuid().ToString()),
            stamp);

        var invitation = new TextPart("calendar") { Text = calendar };
        invitation.ContentType.Charset = "utf-8";
        // The MIME part has to declare the same method the body does, or a client reads it as
        // a file rather than an invitation.
        invitation.ContentType.Parameters["method"] = InvitationBuilder.Method;

        var summary = new TextPart("plain")
        {
            Text = Describe(detected)
        };

        // The shape a calendar invitation normally arrives in: the alternative carries the
        // invitation semantics, and the attachment is the copy that a client with no
        // invitation handling can still open. The attachment is what was proven to work by
        // hand, so it stays even though the alternative should make it redundant.
        var body = new MultipartAlternative { summary, invitation };
        var attachment = new MimePart("application", "ics")
        {
            Content = new MimeContent(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(calendar))),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            ContentTransferEncoding = ContentEncoding.Base64,
            FileName = "invite.ics"
        };

        message.Body = new Multipart("mixed") { body, attachment };
        return message;
    }

    private static string Describe(DetectedEvent detected)
    {
        var lines = new List<string>
        {
            detected.Summary,
            $"Starts: {detected.Start:f}"
        };

        if (detected.End is { } end)
        {
            lines.Add($"Ends: {end:f}");
        }

        if (detected.Location is { Length: > 0 } location)
        {
            lines.Add($"Where: {location}");
        }

        lines.Add("");
        lines.Add(detected.Source == EventSource.StructuredData
            ? "Found in this message's own structured data."
            : "Read out of this message's text by a model, so check it before accepting.");

        return string.Join("\n", lines);
    }
}
