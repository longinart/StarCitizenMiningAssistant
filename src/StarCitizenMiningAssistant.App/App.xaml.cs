using System.IO;
using System.Windows;
using StarCitizenMiningAssistant.App.ViewModels;
using StarCitizenMiningAssistant.Core;
using StarCitizenMiningAssistant.Core.Diagnostics;
using StarCitizenMiningAssistant.Infrastructure.Diagnostics;

namespace StarCitizenMiningAssistant.App;

public partial class App : Application
{
    private IDiagnosticLog _log = NullDiagnosticLog.Instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _log = DiagnosticLogFactory.CreateDefault();
        WriteInformation($"{ApplicationInfo.Name} {ApplicationInfo.VersionText} started.");

        var window = new MainWindow(MainWindowViewModel.Create());
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        WriteInformation($"{ApplicationInfo.Name} stopped.");
        base.OnExit(e);
    }

    private void WriteInformation(string message)
    {
        try
        {
            _log.Information(message);
        }
        catch (IOException)
        {
        }
    }
}
