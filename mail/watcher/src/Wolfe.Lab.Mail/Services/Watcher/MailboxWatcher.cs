using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using Wolfe.Lab.Mail.Detection;
using Wolfe.Lab.Mail.Invitations;
using Wolfe.Lab.Mail.Mailbox;

using Microsoft.Extensions.Options;
using Wolfe.Lab.Mail.Services.Ai;

namespace Wolfe.Lab.Mail.Services.Watcher;

/// <summary>
/// Holds a connection open to the inbox and acts on mail as it lands.
/// </summary>
/// <remarks>
/// Uses IDLE rather than polling, for event-driven latency, but IDLE connections are timebound,
/// so it needs a reconnect loop. The watermark says where to resume.
/// </remarks>
internal sealed class MailboxWatcher(
    ILogger<MailboxWatcher> log,
    IOptions<MailboxWatcherOptions> options,
    InvitationSender sender,
    IEventDetector events
)
{
    /// <summary>
    /// Well inside the protocol's ~29 minutes, and inside most servers' patience.
    /// </summary>
    private static readonly TimeSpan IdleLimit = TimeSpan.FromMinutes(9);

    public async Task Run(CancellationToken ct = default)
    {
        var backoff = TimeSpan.FromSeconds(5);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Session(ct);
                backoff = TimeSpan.FromSeconds(5);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception failure)
            {
                log.LogWarning(failure, "Lost the mailbox; reconnecting in {Backoff}.", backoff);
                await Task.Delay(backoff, ct);
                backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 300));
            }
        }
    }

    private async Task Session(CancellationToken ct)
    {
        using var client = new ImapClient();
        // Bridge presents a self-signed certificate on a port reachable only from this Docker
        // network. Verifying it would mean pinning something the bridge regenerates.
        client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        client.CheckCertificateRevocation = false;

        await client.ConnectAsync(options.Value.BridgeHost, options.Value.ImapPort, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(options.Value.Username, options.Value.Password, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);
        log.LogInformation("Watching {Host}:{Port} as {User}.", options.Value.BridgeHost, options.Value.ImapPort, options.Value.Username);

        var watermark = await Resume(inbox, ct);

        while (!ct.IsCancellationRequested)
        {
            watermark = await Drain(inbox, watermark, ct);
            await WaitForMail(client, inbox, ct);
        }
    }

    /// <summary>
    /// Where to start. A first run — or a mailbox the server has renumbered — starts at the
    /// present: the backlog is deliberately not read, because the lab wants the mail that
    /// arrives from now on and reading years of it would send a great many invitations.
    /// </summary>
    private async Task<Watermark> Resume(IMailFolder inbox, CancellationToken ct)
    {
        var stored = await Watermark.Read(options.Value.StatePath, ct);
        if (stored is not null && stored.UidValidity == inbox.UidValidity)
        {
            log.LogInformation("Resuming after UID {Uid}.", stored.LastUid);
            return stored;
        }

        // UidNext is the number the next arrival will get, so one below it is everything that
        // exists now — and it costs no search.
        var here = inbox.UidNext is { Id: > 0 } next ? next.Id - 1 : 0;
        var fresh = new Watermark(inbox.UidValidity, here);
        await fresh.Write(options.Value.StatePath, ct);

        log.LogInformation(stored is null
                ? "No watermark; starting from now at UID {Uid}."
                : "Mailbox renumbered; restarting from now at UID {Uid}.",
            here);

        return fresh;
    }

    private async Task<Watermark> Drain(IMailFolder inbox, Watermark watermark, CancellationToken ct)
    {
        var range = new UniqueIdRange(new UniqueId(watermark.LastUid + 1), UniqueId.MaxValue);
        var arrived = await inbox.SearchAsync(SearchQuery.Uids(range), ct);

        foreach (var uid in arrived.OrderBy(id => id.Id))
        {
            var message = await inbox.GetMessageAsync(uid, ct);
            await Consider(message, ct);

            // After the send, not before: a crash between the two re-sends an invitation,
            // which is an annoyance, where the other order loses one silently.
            watermark = watermark with { LastUid = uid.Id };
            await watermark.Write(options.Value.StatePath, ct);
        }

        return watermark;
    }

    private async Task Consider(MimeMessage message, CancellationToken ct)
    {
        // Proton already offers to add an event that arrives as an invitation, so touching
        // these would only duplicate what the mail client does natively.
        if (message.BodyParts.Any(part => part.ContentType.IsMimeType("text", "calendar")))
        {
            log.LogDebug("{Subject} is already an invitation.", message.Subject);
            return;
        }

        var detected = StructuredEvents.Read(message.HtmlBody).FirstOrDefault();

        if (detected is null && MessageText.Prose(message) is { Length: > 0 } text)
        {
            detected = await events.Detect(text, message.Date, ct);
        }

        if (detected is null)
        {
            log.LogDebug("No event in {Subject}.", message.Subject);
            return;
        }

        await sender.Send(detected, message, ct);
    }

    /// <summary>
    /// Waits for the server to say something arrived, or for the idle to age out.
    /// </summary>
    private static async Task WaitForMail(ImapClient client, IMailFolder inbox, CancellationToken ct)
    {
        if (!client.Capabilities.HasFlag(ImapCapabilities.Idle))
        {
            // Nothing in the lab should hit this — Bridge supports IDLE — but a server that
            // does not must not become a busy loop.
            await Task.Delay(TimeSpan.FromMinutes(1), ct);
            return;
        }

        using var expiry = new CancellationTokenSource(IdleLimit);
        using var done = CancellationTokenSource.CreateLinkedTokenSource(expiry.Token);

        inbox.CountChanged += Arrived;
        try
        {
            await client.IdleAsync(done.Token, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Either mail arrived or the idle aged out; both mean "go round again".
        }
        finally
        {
            inbox.CountChanged -= Arrived;
        }

        return;

        void Arrived(object? s, EventArgs e) => done.Cancel();
    }
}
