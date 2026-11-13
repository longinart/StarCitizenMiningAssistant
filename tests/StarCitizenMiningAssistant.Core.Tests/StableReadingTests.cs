using StarCitizenMiningAssistant.Core.Calculation;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class StableReadingTests
{
    [Fact]
    public void Two_identical_reads_confirm_and_one_read_stays_uncertain()
    {
        var first = StableReading.Next(ObservedValue<double>.Missing(), null, ObservedValue<double>.Uncertain(5000));
        var second = StableReading.Next(first.Shown, first.LastSample, ObservedValue<double>.Uncertain(5000));
        var different = StableReading.Next(second.Shown, second.LastSample, ObservedValue<double>.Uncertain(5100));

        Assert.Equal(RecognitionConfidence.Uncertain, first.Shown.Confidence);
        Assert.Equal(5000, first.Shown.Value);
        Assert.True(second.Shown.IsConfirmed);
        Assert.Equal(5000, second.Shown.Value);
        Assert.Equal(RecognitionConfidence.Uncertain, different.Shown.Confidence);
        Assert.Equal(5100, different.Shown.Value);
    }

    [Fact]
    public void A_dark_frame_between_two_identical_reads_still_confirms_the_second()
    {
        var first = StableReading.Next(ObservedValue<double>.Missing(), null, ObservedValue<double>.Uncertain(40));
        var dark = StableReading.Next(first.Shown, first.LastSample, ObservedValue<double>.Missing());
        var second = StableReading.Next(dark.Shown, dark.LastSample, ObservedValue<double>.Uncertain(40));

        Assert.Equal(first.Shown, dark.Shown);
        Assert.Equal(first.LastSample, dark.LastSample);
        Assert.True(second.Shown.IsConfirmed);
        Assert.Equal(40, second.Shown.Value);
    }

    [Fact]
    public void A_typed_value_stays_confirmed_when_the_first_sample_matches_it()
    {
        var typed = ObservedValue<double>.Confirmed(5000);
        var same = StableReading.Next(typed, null, ObservedValue<double>.Uncertain(5000));
        var other = StableReading.Next(typed, null, ObservedValue<double>.Uncertain(4800));

        Assert.True(same.Shown.IsConfirmed);
        Assert.Equal(5000, same.Shown.Value);
        Assert.Equal(RecognitionConfidence.Uncertain, other.Shown.Confidence);
        Assert.Equal(4800, other.Shown.Value);
    }

    [Fact]
    public void One_confirmed_field_does_not_make_the_other_field_crackable()
    {
        var once = StableReading.Next(ObservedValue<double>.Missing(), null, ObservedValue<double>.Uncertain(5000));
        var mass = StableReading.Next(once.Shown, once.LastSample, ObservedValue<double>.Uncertain(5000));
        var resistance = StableReading.Next(ObservedValue<double>.Missing(), null, ObservedValue<double>.Uncertain(0));
        var rock = new RockReading(mass.Shown, resistance.Shown, ObservedValue<double>.Missing());

        Assert.True(rock.MassKilograms.IsConfirmed);
        Assert.False(rock.ResistancePercent.IsConfirmed);
        Assert.False(rock.MassKilograms.IsConfirmed && rock.ResistancePercent.IsConfirmed);
    }
}
