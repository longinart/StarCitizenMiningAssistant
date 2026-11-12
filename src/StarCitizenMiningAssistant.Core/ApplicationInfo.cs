using System.Reflection;

namespace StarCitizenMiningAssistant.Core;

public static class ApplicationInfo
{
    public const string Name = "Star Citizen Mining Assistant";

    public static Version Version { get; } =
        typeof(ApplicationInfo).Assembly.GetName().Version ?? new Version(0, 1, 0);

    public static string VersionText => Version.ToString(3);
}
