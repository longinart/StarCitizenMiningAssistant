using StarCitizenMiningAssistant.Core;
using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class ObservedValueTests
{
    [Fact]
    public void Uncertain_reading_is_not_confirmed()
    {
        var reading = ObservedValue<string>.Uncertain("Quantainium");

        Assert.False(reading.IsConfirmed);
        Assert.Equal(RecognitionConfidence.Uncertain, reading.Confidence);
        Assert.Equal("Quantainium", reading.Value);
    }

    [Fact]
    public void Confirmed_reading_is_confirmed()
    {
        var reading = ObservedValue<int>.Confirmed(40);

        Assert.True(reading.IsConfirmed);
        Assert.Equal(40, reading.Value);
    }

    [Fact]
    public void Missing_numeric_reading_is_not_confirmed()
    {
        var reading = ObservedValue<int>.Missing();

        Assert.Equal(RecognitionConfidence.Missing, reading.Confidence);
        Assert.False(reading.IsConfirmed);
    }

    [Fact]
    public void Confirmed_rejects_a_null_reference()
    {
        Assert.Throws<ArgumentNullException>(() => ObservedValue<string>.Confirmed(null!));
    }

    [Fact]
    public void Application_identity_is_stable()
    {
        Assert.Equal("Star Citizen Mining Assistant", ApplicationInfo.Name);
        Assert.Equal("0.1.0", ApplicationInfo.VersionText);
    }
}
