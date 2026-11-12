using StarCitizenMiningAssistant.Core;

namespace StarCitizenMiningAssistant.App.ViewModels;

public sealed class MainWindowViewModel
{
    public MainWindowViewModel(string windowTitle, string productName, string versionText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(windowTitle);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentException.ThrowIfNullOrWhiteSpace(versionText);

        WindowTitle = windowTitle;
        ProductName = productName;
        VersionText = versionText;
    }

    public string WindowTitle { get; }

    public string ProductName { get; }

    public string VersionText { get; }

    public static MainWindowViewModel Create() =>
        new(ApplicationInfo.Name, ApplicationInfo.Name, "Version " + ApplicationInfo.VersionText);
}
