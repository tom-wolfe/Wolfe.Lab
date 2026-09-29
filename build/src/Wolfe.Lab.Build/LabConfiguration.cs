using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;

namespace Wolfe.Lab.Build;

/// <summary>
/// How the CLI behaves: <c>appsettings.json</c>, shipped beside it in the tool, overridden per
/// node by environment variables prefixed <c>LAB_</c>.
/// </summary>
/// <remarks>
/// Not what a component is — that is its <c>ritten.json</c> — but the limits, intervals and
/// timeouts of the clients that do the work. Read once per process.
/// </remarks>
public static class LabConfiguration
{
    internal const string EnvironmentPrefix = "LAB_";

    private static readonly Lazy<IConfiguration> Loaded = new(() => Load(AppContext.BaseDirectory));

    /// <summary>
    /// The configuration beside the CLI in <paramref name="directory"/>, with the environment over it.
    /// </summary>
    internal static IConfiguration Load(string directory) => new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(directory, "appsettings.json"), optional: false)
        .AddEnvironmentVariables(EnvironmentPrefix)
        .Build();

    /// <summary>
    /// The configuration, loaded on first use.
    /// </summary>
    public static IConfiguration Current => Loaded.Value;

    extension(IWorkflowBuilder builder)
    {
        /// <summary>
        /// Registers the lab configuration.
        /// </summary>
        public IWorkflowBuilder AddLabConfiguration()
        {
            builder.Services.TryAddSingleton(Current);
            return builder;
        }
    }
}
