using System.IO;
using System.Text.Json;
using System.Windows;
using StarCitizenMiningAssistant.App.ViewModels;
using StarCitizenMiningAssistant.Core;
using StarCitizenMiningAssistant.Core.Diagnostics;
using StarCitizenMiningAssistant.Core.Equipment;
using StarCitizenMiningAssistant.Infrastructure.Diagnostics;
using StarCitizenMiningAssistant.Infrastructure.Persistence;

namespace StarCitizenMiningAssistant.App;

public partial class App : Application
{
    private IDiagnosticLog _log = NullDiagnosticLog.Instance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _log = DiagnosticLogFactory.CreateDefault();
        WriteInformation($"{ApplicationInfo.Name} {ApplicationInfo.VersionText} started.");

        var window = new MainWindow(CreateViewModel());
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        WriteInformation($"{ApplicationInfo.Name} stopped.");
        base.OnExit(e);
    }

    private MainWindowViewModel CreateViewModel()
    {
        var catalogPath = Path.Combine(AppContext.BaseDirectory, "data", "mining-equipment.json");
        try
        {
            var catalog = MiningCatalog.Parse(File.ReadAllText(catalogPath));
            WriteInformation("Loaded equipment catalog " + catalog.GameVersion + ".");
            var viewModel = MainWindowViewModel.Create(catalog);
            var saved = LoadoutFile.TryLoad();
            if (saved is not null && !string.IsNullOrWhiteSpace(saved.ShipId))
            {
                viewModel.Restore(saved);
            }

            viewModel.LoadoutChanged += (_, _) => SaveLoadout(viewModel);
            return viewModel;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            WriteInformation("Equipment catalog failed to load: " + ex.Message);
            return MainWindowViewModel.CreateMissing(ex.Message);
        }
    }

    private void SaveLoadout(MainWindowViewModel viewModel)
    {
        try
        {
            LoadoutFile.Save(viewModel.CaptureLoadout());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteInformation("Loadout was not saved: " + ex.Message);
        }
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
