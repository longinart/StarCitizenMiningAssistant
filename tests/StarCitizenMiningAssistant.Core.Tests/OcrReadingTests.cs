using System.Globalization;
using StarCitizenMiningAssistant.Core.Capture;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class OcrReadingTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Fact]
    public void A_perfect_confidence_score_stays_uncertain()
    {
        var document = OcrDocument.FromEngine("en", [new OcrFragment("Stileron", 1)]);

        var line = Assert.Single(document.Lines);
        Assert.Equal("Stileron", line.Value);
        Assert.Equal(RecognitionConfidence.Uncertain, line.Confidence);
        Assert.False(line.IsConfirmed);
    }

    [Fact]
    public void Blank_text_is_left_out()
    {
        var document = OcrDocument.FromEngine("en", [new OcrFragment("  ", null), new OcrFragment("Quantainium", null)]);

        var line = Assert.Single(document.Lines);
        Assert.Equal("Quantainium", line.Value);
    }

    [Fact]
    public void Rock_numbers_keep_the_line_uncertain_and_ignore_an_unlabelled_number()
    {
        var lines = new[]
        {
            ObservedValue<string>.Uncertain("Signature 4400"),
            ObservedValue<string>.Uncertain("Mass 31,300 kg"),
            ObservedValue<string>.Uncertain("Resistance 50%"),
            ObservedValue<string>.Uncertain("Instability 1.5")
        };

        var rock = RockOcrFields.Read(lines, English);

        Assert.False(rock.Mass.IsConfirmed);
        Assert.Equal(31300, rock.Mass.Value);
        Assert.Equal(RecognitionConfidence.Uncertain, rock.Resistance.Confidence);
        Assert.Equal(50, rock.Resistance.Value);
        Assert.Equal(1.5, rock.Instability.Value);
    }

    [Fact]
    public void A_missing_label_stays_missing_and_a_confirmed_line_is_not_downgraded()
    {
        var lines = new[]
        {
            ObservedValue<string>.Confirmed("Mass 5000"),
            ObservedValue<string>.Uncertain("The rock looks large")
        };

        var rock = RockOcrFields.Read(lines, English);

        Assert.True(rock.Mass.IsConfirmed);
        Assert.Equal(5000, rock.Mass.Value);
        Assert.Equal(RecognitionConfidence.Missing, rock.Resistance.Confidence);
        Assert.Equal(RecognitionConfidence.Missing, rock.Instability.Confidence);
    }

    [Fact]
    public void Czech_decimal_comma_parses_resistance()
    {
        var lines = new[] { ObservedValue<string>.Uncertain("Resistance 31,5%") };

        var rock = RockOcrFields.Read(lines, CultureInfo.GetCultureInfo("cs-CZ"));

        Assert.Equal(31.5, rock.Resistance.Value);
        Assert.Equal(RecognitionConfidence.Uncertain, rock.Resistance.Confidence);
    }

    [Fact]
    public void The_scan_panel_is_mass_and_resistance_and_composition_is_left_out()
    {
        var lines = new[]
        {
            ObservedValue<string>.Uncertain("SCAN RESULTS"),
            ObservedValue<string>.Uncertain("COPPER (ORE)"),
            ObservedValue<string>.Uncertain("MASS 5386"),
            ObservedValue<string>.Uncertain("RESISTANCE 8%"),
            ObservedValue<string>.Uncertain("INSTABILITY 5.32"),
            ObservedValue<string>.Uncertain("61.28% COPPER (ORE)"),
            ObservedValue<string>.Uncertain("CARGO 9.09 / 64.00 SCU")
        };

        var rock = RockOcrFields.Read(lines, English);

        Assert.True(rock.HasPanel);
        Assert.Equal(5386, rock.Mass.Value);
        Assert.Equal(8, rock.Resistance.Value);
        Assert.Equal(5.32, rock.Instability.Value);
        var record = ScanLogText.Rock(lines);
        Assert.Equal(
            "mass=5386/Uncertain resistance=8/Uncertain instability=5.32/Uncertain",
            record.Reading);
        Assert.Equal("", record.Detail);
    }

    [Fact]
    public void Composition_without_the_two_crack_numbers_is_not_a_panel()
    {
        var lines = new[]
        {
            ObservedValue<string>.Uncertain("61.28% COPPER (ORE)"),
            ObservedValue<string>.Uncertain("INERT MATERIALS 29.88%")
        };

        var rock = RockOcrFields.Read(lines, English);

        Assert.False(rock.HasPanel);
        Assert.Equal("", ScanLogText.Rock(lines).Reading);
    }

    [Fact]
    public void A_number_beside_the_label_is_kept_and_composition_is_not()
    {
        var words = new[]
        {
            new RockPanel.Word("HASS:", 40, 80, 12),
            new RockPanel.Word("5380", 180, 78, 12),
            new RockPanel.Word("RESISTANCE:", 40, 110, 12),
            new RockPanel.Word("0%", 180, 112, 12),
            new RockPanel.Word("INSTABILITY:", 40, 140, 12),
            new RockPanel.Word("5.32", 180, 140, 12),
            new RockPanel.Word("COPPER", 40, 200, 12),
            new RockPanel.Word("61.28%", 40, 220, 12)
        };

        var rock = RockPanel.Read(words, []);

        Assert.Equal(5380, rock.Mass.Value);
        Assert.Equal(0, rock.Resistance.Value);
        Assert.Equal(5.32, rock.Instability.Value);
        Assert.False(rock.Mass.IsConfirmed);
    }

    [Fact]
    public void A_parsed_number_beats_a_short_shape_and_a_percent_sign_is_not_part_of_it()
    {
        var words = new[]
        {
            new RockPanel.Word("HAS:", 40, 80, 12),
            new RockPanel.Word("51296", 180, 80, 12),
            new RockPanel.Word("RESISTANCE:", 40, 104, 12),
            new RockPanel.Word("47%", 180, 104, 12),
            new RockPanel.Word("INSTABILITY:", 40, 128, 12),
            new RockPanel.Word("1S9.a0", 180, 128, 12)
        };
        var amounts = new[]
        {
            new RockDigitReader.Amount("5", 80, 180),
            new RockDigitReader.Amount("4", 104, 180),
            new RockDigitReader.Amount("159.00", 128, 180)
        };

        var rock = RockPanel.Read(words, amounts);

        Assert.Equal(51296, rock.Mass.Value);
        Assert.Equal(47, rock.Resistance.Value);
        Assert.Equal(159, rock.Instability.Value);
    }

    [Fact]
    public void An_eight_read_as_zero_by_the_text_is_corrected_by_the_glyph()
    {
        var words = new[]
        {
            new RockPanel.Word("MASS:", 40, 80, 12),
            new RockPanel.Word("6029", 180, 80, 12)
        };
        var amounts = new[] { new RockDigitReader.Amount("6829", 80, 180) };

        var rock = RockPanel.Read(words, amounts);

        Assert.Equal(6829, rock.Mass.Value);
    }

    [Fact]
    public void A_longer_digit_row_beats_a_short_text_reading()
    {
        var one = RockPanel.Read(
            [new RockPanel.Word("MASS:", 40, 80, 12), new RockPanel.Word("1", 180, 80, 12)],
            [new RockDigitReader.Amount("10895", 80, 180, 5)]);
        var dropped = RockPanel.Read(
            [new RockPanel.Word("MASS:", 40, 80, 12), new RockPanel.Word("302", 180, 80, 12)],
            [new RockDigitReader.Amount("3402", 80, 180, 4)]);

        Assert.Equal(10895, one.Mass.Value);
        Assert.Equal(3402, dropped.Mass.Value);
    }

    [Fact]
    public void A_short_fragment_is_blank_when_the_row_had_more_digits()
    {
        var rock = RockPanel.Read(
            [new RockPanel.Word("MASS:", 40, 80, 12), new RockPanel.Word("302", 180, 80, 12)],
            [new RockDigitReader.Amount("", 80, 180, 4)]);

        Assert.Equal(RecognitionConfidence.Missing, rock.Mass.Confidence);
    }

    [Fact]
    public void One_read_number_is_still_a_panel()
    {
        var lines = new[] { ObservedValue<string>.Uncertain("MASS 51296") };

        var rock = RockOcrFields.Read(lines, English);

        Assert.True(rock.HasPanel);
        Assert.Equal(51296, rock.Mass.Value);
        Assert.Equal(RecognitionConfidence.Missing, rock.Resistance.Confidence);
    }

    [Fact]
    public void The_rock_patch_covers_the_measured_header_range_in_one_step()
    {
        var home = RockPatch.Of(2560, 1440);
        Assert.Equal(1638, home.X);
        Assert.Equal(590, home.Y);
        Assert.InRange(614, home.Y, home.Y + home.Height);
        Assert.InRange(754 + 110, home.Y, home.Y + home.Height);
        var down = RockPatch.Shifted(2560, 1440, 0, 100);
        Assert.NotNull(down);
        Assert.InRange(754, down.Value.Y, down.Value.Y + down.Value.Height);
        Assert.Null(RockPatch.Shifted(2560, 1440, 0, -600));
    }

    [Fact]
    public void A_rock_step_that_leaves_the_monitor_is_skipped()
    {
        var placed = new CaptureRectangle { Left = 40, Top = 200, Width = 280, Height = 160 };

        Assert.Null(placed.Shifted(2560, 1440, -100, 0));
        var shifted = placed.Shifted(2560, 1440, 100, 0);

        Assert.NotNull(shifted);
        Assert.Equal(140, shifted.Left);
        Assert.Equal(200, shifted.Top);
        Assert.Equal(280, shifted.Width);
        Assert.Equal(160, shifted.Height);
        Assert.Equal(40, placed.Left);
    }
}
