using Wolfe.Lab.Infrastructure.Packages;

namespace Wolfe.Lab.Application.Packages;

/// <summary>
/// How an installed package reads in the run's report and log.
/// </summary>
internal static class Packages
{
    internal const string Section = "Packages";

    internal static void Report(IWorkflowReport report, IWorkflowLog log, InstalledPackage installed)
    {
        var package = installed.Package;
        var what = $"`{package.Repository}` {package.Tag}";
        var section = report.Section(Section);
        switch (installed.Outcome)
        {
            case PackageOutcome.Installed:
                section.Success($"Installed {what} for {package.Name}, checked against {package.Verification}.");
                log.Status($"Installed {package.Repository} {package.Tag} into {installed.Directory.AbsolutePath}.");
                break;
            case PackageOutcome.WouldInstall:
                section.Note($"Would install {what} for {package.Name}.");
                break;
            default:
                section.Note($"{package.Name}: {what}, already installed.");
                log.Detail($"{package.Name} is {package.Repository} {package.Tag}, already installed.");
                break;
        }
    }
}
