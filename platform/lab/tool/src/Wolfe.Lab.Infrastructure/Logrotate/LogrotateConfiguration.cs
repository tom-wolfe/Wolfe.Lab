using System.Text;

namespace Wolfe.Lab.Infrastructure.Logrotate;

/// <summary>
/// A logrotate configuration for a set of logs, each kept to <see cref="Size"/> and
/// <see cref="Generations"/> compressed generations.
/// </summary>
/// <remarks>
/// copytruncate, because launchd and systemd open a log once and write to it for as long as the
/// agent runs: a log moved aside would go on being written to under its old name. The copy and
/// the truncate are not atomic, so a line written between the two can be lost — the price of not
/// restarting the agent.
/// </remarks>
public static class LogrotateConfiguration
{
    /// <summary>
    /// The size a log is rotated at.
    /// </summary>
    public const string Size = "10M";

    /// <summary>
    /// How many rotated logs are kept, compressed.
    /// </summary>
    public const int Generations = 5;

    /// <summary>
    /// The configuration that rotates <paramref name="logs"/>.
    /// </summary>
    public static string For(IEnumerable<IFile> logs)
    {
        var text = new StringBuilder();
        foreach (var log in logs)
        {
            text.Append('"').Append(log.AbsolutePath).AppendLine("\" {")
                .AppendLine($"    size {Size}")
                .AppendLine($"    rotate {Generations}")
                .AppendLine("    copytruncate")
                .AppendLine("    compress")
                .AppendLine("    delaycompress")
                .AppendLine("    missingok")
                .AppendLine("    notifempty")
                .AppendLine("}");
        }

        return text.ToString();
    }
}
