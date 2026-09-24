using Scriban;
using Scriban.Runtime;

namespace Wolfe.Lab.Build.Clients.Agents;

/// <summary>
/// The unit templates every supervisor renders through.
/// </summary>
internal static class UnitTemplate
{
    /// <summary>
    /// Loads an embedded template. A template that does not parse is a packaging fault rather than
    /// anything a node did, so it is thrown rather than reported.
    /// </summary>
    /// <param name="name">The template's file name under <c>Templates/</c>.</param>
    public static Template Load(string name)
    {
        using var stream = typeof(UnitTemplate).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"{name} is not in the assembly: the template was not embedded.");
        using var reader = new StreamReader(stream);
        var template = Template.Parse(reader.ReadToEnd());
        return template.HasErrors
            ? throw new InvalidOperationException($"{name} does not parse: {string.Join("; ", template.Messages)}")
            : template;
    }

    /// <summary>
    /// Renders a model through a template under its own member names, so the template reads as
    /// the record beside it rather than in a second naming convention.
    /// </summary>
    /// <param name="template">The template.</param>
    /// <param name="model">The unit record, every value already escaped for its format.</param>
    public static string Fill(Template template, object model)
    {
        var globals = new ScriptObject();
        globals.Import(model, renamer: member => member.Name);
        var context = new TemplateContext { MemberRenamer = member => member.Name };
        context.PushGlobal(globals);
        return template.Render(context).TrimEnd('\n') + '\n';
    }
}
