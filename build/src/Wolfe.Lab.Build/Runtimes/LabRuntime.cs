using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ritten.Forgejo;
using Ritten.Reporting.Sinks;
using Wolfe.Lab.Build.Alerts;

namespace Wolfe.Lab.Build.Runtimes;

/// <summary>
/// A job on one of the lab's runners: Forgejo Actions as Ritten knows it, plus the lab's own
/// concern — nobody is watching, so a failure has to reach a phone.
/// </summary>
public sealed class LabRuntime : ForgejoActionsRuntime
{
    /// <inheritdoc />
    public override void Configure(IWorkflowBuilder builder, Func<string, string?> environment)
    {
        base.Configure(builder, environment);
        builder.AddAlerts();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWorkflowResultSink, AlertOnFailure>());
    }
}
