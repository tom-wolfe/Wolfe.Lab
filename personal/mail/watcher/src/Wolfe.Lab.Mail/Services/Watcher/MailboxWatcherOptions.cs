namespace Wolfe.Lab.Mail.Services.Watcher;

/// <summary>
/// Everything the watcher is told.
/// </summary>
internal sealed class MailboxWatcherOptions
{
    /// <summary>
    /// The configuration section these are bound from, and the prefix of every variable that
    /// fills them.
    /// </summary>
    internal const string Section = "Watcher";

    /// <summary>
    /// Where Proton Bridge answers on the lab network.
    /// </summary>
    public string BridgeHost { get; set; } = "bridge";

    /// <summary>
    /// Bridge's IMAP port.
    /// </summary>
    public int ImapPort { get; set; } = 143;

    /// <summary>
    /// Bridge's SMTP port.
    /// </summary>
    public int SmtpPort { get; set; } = 25;

    /// <summary>
    /// Bridge's own generated username, not the Proton account's.
    /// </summary>
    public string Username { get; set; } = "";

    /// <summary>
    /// Bridge's own generated password.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// Where invitations are sent. Left unset, it is the mailbox's own address.
    /// </summary>
    public string Recipient { get; set; } = "";

    /// <summary>
    /// The watermark file.
    /// </summary>
    public string StatePath { get; set; } = "/state/watermark.json";
}
