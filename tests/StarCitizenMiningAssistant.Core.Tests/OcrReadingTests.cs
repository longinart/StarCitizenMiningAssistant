using System.Globalization;
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
}
