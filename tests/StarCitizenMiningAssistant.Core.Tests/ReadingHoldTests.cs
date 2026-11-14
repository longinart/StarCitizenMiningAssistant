using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class ReadingHoldTests
{
    [Fact]
    public void A_dark_frame_keeps_the_confirmed_signature()
    {
        var confirmed = ObservedValue<double>.Confirmed(7200);
        var held = ReadingHold.Next(confirmed, ObservedValue<double>.Missing());
        var still = ReadingHold.Next(held, ObservedValue<double>.Missing());

        Assert.Equal(confirmed, held);
        Assert.Equal(confirmed, still);
    }

    [Fact]
    public void A_different_number_replaces_the_held_signature()
    {
        var confirmed = ObservedValue<double>.Confirmed(7200);
        var next = ReadingHold.Next(confirmed, ObservedValue<double>.Confirmed(10800));
        var uncertain = ReadingHold.Next(next, ObservedValue<double>.Uncertain(7201));

        Assert.True(next.IsConfirmed);
        Assert.Equal(10800, next.Value);
        Assert.Equal(RecognitionConfidence.Uncertain, uncertain.Confidence);
        Assert.Equal(7201, uncertain.Value);
    }

    [Fact]
    public void A_dark_frame_also_keeps_an_uncertain_number()
    {
        var uncertain = ObservedValue<double>.Uncertain(7201);
        var held = ReadingHold.Next(uncertain, ObservedValue<double>.Missing());

        Assert.Equal(uncertain, held);
        Assert.Equal(ObservedValue<double>.Missing(), ReadingHold.Next(ObservedValue<double>.Missing(), ObservedValue<double>.Missing()));
    }
}
