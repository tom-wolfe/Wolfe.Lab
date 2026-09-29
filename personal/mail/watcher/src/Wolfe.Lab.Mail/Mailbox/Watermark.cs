using System.Text.Json;

namespace Wolfe.Lab.Mail.Mailbox;

/// <summary>
/// How far through the mailbox the watcher has read.
/// </summary>
/// <remarks>
/// A UID rather than a date, because IMAP's date search has DAY granularity — a date
/// watermark would re-read the whole of today, all day. UIDs are monotonic within a mailbox,
/// which is exactly the question being asked, and <c>UidValidity</c> is the guard: when the
/// server renumbers, every stored UID means something else and the only safe move is to start
/// again from now.
/// </remarks>
/// <param name="UidValidity">The mailbox generation the UID belongs to.</param>
/// <param name="LastUid">The highest UID already dealt with.</param>
internal sealed record Watermark(uint UidValidity, uint LastUid)
{
    public static async Task<Watermark?> Read(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Watermark>(stream, cancellationToken: ct);
    }

    /// <summary>
    /// Written through a temporary file and moved into place: a watcher killed mid-write must
    /// not leave a half-written watermark, which would read as "start from nothing".
    /// </summary>
    public async Task Write(string path, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var temporary = path + ".tmp";

        await using (var stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, this, cancellationToken: ct);
        }

        File.Move(temporary, path, overwrite: true);
    }
}
