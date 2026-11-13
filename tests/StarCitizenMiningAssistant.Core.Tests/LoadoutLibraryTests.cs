using StarCitizenMiningAssistant.Core.Capture;
using StarCitizenMiningAssistant.Core.Equipment;

namespace StarCitizenMiningAssistant.Core.Tests;

public class LoadoutLibraryTests
{
    [Fact]
    public void Saving_a_second_name_leaves_the_first_loadout_unchanged()
    {
        var library = new LoadoutLibrary();
        Assert.True(library.SaveNamed("Cleanup", Screen("argo-mole", "hofstede-s2-mining-laser", "focus-iii-module", "xtr-xl-module")));

        var heavy = Screen("argo-mole", "helix-ii-mining-laser", "xtr-xl-module", "surge-module", "rieger-c3-module");
        library.RememberDraft(heavy);
        Assert.True(library.SaveNamed("Heavy", heavy));

        var cleanup = library.Find("Cleanup");
        var storedHeavy = library.Find("Heavy");
        Assert.NotNull(cleanup);
        Assert.NotNull(storedHeavy);
        Assert.Equal("hofstede-s2-mining-laser", cleanup.Turrets[0].HeadId);
        Assert.Equal(["focus-iii-module", "xtr-xl-module"], cleanup.Turrets[0].Modules);
        Assert.Equal("helix-ii-mining-laser", storedHeavy.Turrets[0].HeadId);
        Assert.Equal(["xtr-xl-module", "surge-module", "rieger-c3-module"], storedHeavy.Turrets[0].Modules);
        Assert.Equal("Heavy", library.ActiveName);
    }

    [Fact]
    public void Editing_the_draft_does_not_change_a_saved_name()
    {
        var library = new LoadoutLibrary();
        library.SaveNamed("Cleanup", Screen("argo-mole", "hofstede-s2-mining-laser", "focus-iii-module"));

        var edited = Screen("argo-mole", "helix-ii-mining-laser", "surge-module");
        library.RememberDraft(edited);
        edited.Turrets[0].Modules.Add("rieger-c3-module");

        Assert.Equal(["focus-iii-module"], library.Find("Cleanup")!.Turrets[0].Modules);
        Assert.Equal(["surge-module"], library.Draft!.Turrets[0].Modules);
    }

    [Fact]
    public void An_old_single_loadout_file_becomes_one_named_loadout()
    {
        const string json = """
            {
              "shipId": "argo-mole",
              "gadgetId": "sabir",
              "massText": "31300",
              "resistanceText": "50",
              "instabilityText": "",
              "signatureText": "",
              "turrets": [
                { "headId": "helix-ii-mining-laser", "laserOn": true, "craftPercent": "0", "modules": [ "rieger-c3-module", "xtr-xl-module", "surge-module" ] }
              ],
              "trackedMinerals": [ "Stileron" ]
            }
            """;

        var library = LoadoutLibrary.TryParse(json);

        Assert.NotNull(library);
        Assert.Equal("Loadout", library.ActiveName);
        Assert.Equal("sabir", library.Find("Loadout")!.GadgetId);
        Assert.Equal("helix-ii-mining-laser", library.Draft!.Turrets[0].HeadId);
        Assert.Equal(["Stileron"], library.TrackedMinerals);
        Assert.Equal("31300", library.MassText);
    }

    [Fact]
    public void A_saved_library_round_trips_without_mixing_modules()
    {
        var library = new LoadoutLibrary();
        library.SaveNamed("Cleanup", Screen("argo-mole", "hofstede-s2-mining-laser", "focus-iii-module"));
        library.SaveNamed("Heavy", Screen("argo-mole", "helix-ii-mining-laser", "surge-module"));

        var restored = LoadoutLibrary.TryParse(library.ToJson());

        Assert.NotNull(restored);
        Assert.Equal("Heavy", restored.ActiveName);
        Assert.Equal(["focus-iii-module"], restored.Find("Cleanup")!.Turrets[0].Modules);
        Assert.Equal(["surge-module"], restored.Find("heavy")!.Turrets[0].Modules);
    }

    [Fact]
    public void Overlay_position_is_kept_in_the_saved_file()
    {
        var library = new LoadoutLibrary
        {
            OverlayLeft = 120,
            OverlayTop = 80,
            OverlayWidth = 440,
            OverlayHeight = 720
        };

        var restored = LoadoutLibrary.TryParse(library.ToJson());

        Assert.NotNull(restored);
        Assert.Equal(120, restored.OverlayLeft);
        Assert.Equal(80, restored.OverlayTop);
        Assert.Equal(440, restored.OverlayWidth);
        Assert.Equal(720, restored.OverlayHeight);
    }

    [Fact]
    public void A_rectangle_on_one_monitor_leaves_the_other_monitor_and_the_other_area()
    {
        var library = new LoadoutLibrary();
        var signatures = new CaptureRectangle { Left = 10, Top = 20, Width = 300, Height = 80 };
        var rock = new CaptureRectangle { Left = 40, Top = 400, Width = 260, Height = 140 };
        Assert.True(library.RememberCapture(@"\\.\DISPLAY1", CaptureTarget.Signatures, signatures));
        Assert.True(library.RememberCapture(@"\\.\DISPLAY1", CaptureTarget.Rock, rock));
        Assert.True(library.RememberCapture(@"\\.\DISPLAY3", CaptureTarget.Signatures, new CaptureRectangle
        {
            Left = 8,
            Top = 8,
            Width = 100,
            Height = 40
        }));

        signatures.Left = 999;
        var display1 = library.FindCapture(@"\\.\display1");
        var display3 = library.FindCapture(@"\\.\DISPLAY3");
        Assert.NotNull(display1);
        Assert.NotNull(display3);
        Assert.Equal(10, display1.Signatures!.Left);
        Assert.Equal(40, display1.Rock!.Left);
        Assert.Null(display3.Rock);
        Assert.Equal(8, display3.Signatures!.Left);
        Assert.False(library.RememberCapture(@"\\.\DISPLAY1", CaptureTarget.Rock, new CaptureRectangle
        {
            Left = 0,
            Top = 0,
            Width = 7,
            Height = 40
        }));
        Assert.Equal(40, library.FindCapture(@"\\.\DISPLAY1")!.Rock!.Left);
    }

    [Fact]
    public void Capture_rectangles_survive_the_saved_file()
    {
        var library = new LoadoutLibrary();
        library.RememberCapture(@"\\.\DISPLAY3", CaptureTarget.Rock, new CaptureRectangle
        {
            Left = 12,
            Top = 34,
            Width = 200,
            Height = 90
        });

        var restored = LoadoutLibrary.TryParse(library.ToJson());

        Assert.NotNull(restored);
        var rock = restored.FindCapture(@"\\.\DISPLAY3")!.Rock;
        Assert.NotNull(rock);
        Assert.Equal(12, rock.Left);
        Assert.Equal(34, rock.Top);
        Assert.Equal(200, rock.Width);
        Assert.Equal(90, rock.Height);
        Assert.Null(restored.FindCapture(@"\\.\DISPLAY3")!.Signatures);
    }

    private static StoredLoadout Screen(string shipId, string headId, params string[] modules) =>
        new()
        {
            ShipId = shipId,
            Turrets =
            [
                new StoredTurret
                {
                    HeadId = headId,
                    LaserOn = true,
                    CraftPercent = "0",
                    Modules = modules.Cast<string?>().ToList()
                }
            ]
        };
}
