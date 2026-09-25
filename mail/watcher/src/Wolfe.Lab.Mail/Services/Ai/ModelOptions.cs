namespace Wolfe.Lab.Mail.Services.Ai;

/// <summary>
/// The model that reads prose.
/// </summary>
internal sealed class ModelOptions
{
    /// <summary>
    /// The configuration section these are bound from, and the prefix of every variable that
    /// fills them.
    /// </summary>
    internal const string Section = "Model";

    /// <summary>
    /// An OpenAI-compatible host, so ollama and most hosted APIs both fit. Which API path lives
    /// under it is the client's business, not the deployment's.
    /// </summary>
    public string Endpoint { get; set; } = "";

    /// <summary>
    /// The model to ask for — a role such as <c>lab/background</c>.
    /// </summary>
    public string Name { get; set; } = "";
}
