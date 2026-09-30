using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Wolfe.Lab.Build.Clients.Agents.Launchd;

/// <summary>
/// Apple's property list format, written with an XML writer.
/// </summary>
internal static class Plist
{
    private static readonly XmlWriterSettings Settings = new()
    {
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
        Indent = true,
        IndentChars = "\t",
        NewLineChars = "\n"
    };

    /// <summary>
    /// A property list document around <paramref name="root"/>, with a comment above it.
    /// </summary>
    public static XDocument Document(string comment, XElement root) => new(
        new XDeclaration("1.0", "UTF-8", null),
        new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
        new XComment($"\n{comment}\n"),
        new XElement("plist", new XAttribute("version", "1.0"), root));

    public static XElement String(string value) => new("string", value);

    public static XElement Boolean(bool value) => new(value ? "true" : "false");

    /// <summary>
    /// The document as launchd reads it: UTF-8 without a byte order mark, tab-indented.
    /// </summary>
    public static string Write(XDocument document)
    {
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, Settings))
        {
            document.Save(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + '\n';
    }
}
