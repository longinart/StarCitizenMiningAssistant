using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class ReadingStabilizerTests
{
    [Fact]
    public void Five_identical_readings_are_accepted()
    {
        var stabilizer = new ReadingStabilizer<string>(5);
        Assert.False(stabilizer.Observe("Tin 8.2"));
        Assert.False(stabilizer.Observe("Tin 8.2"));
        Assert.False(stabilizer.Observe("Tin 8.2"));
        Assert.False(stabilizer.Observe("Tin 8.2"));
        Assert.True(stabilizer.Observe("Tin 8.2"));
        Assert.True(stabilizer.Observe("Tin 8.2"));
    }

    [Fact]
    public void A_different_reading_starts_the_count_again()
    {
        var stabilizer = new ReadingStabilizer<int>(3);
        Assert.False(stabilizer.Observe(1));
        Assert.False(stabilizer.Observe(1));
        Assert.False(stabilizer.Observe(2));
        Assert.False(stabilizer.Observe(2));
        Assert.True(stabilizer.Observe(2));
    }

    [Fact]
    public void Reset_clears_the_streak()
    {
        var stabilizer = new ReadingStabilizer<string>(2);
        Assert.False(stabilizer.Observe("a"));
        stabilizer.Reset();
        Assert.False(stabilizer.Observe("a"));
        Assert.True(stabilizer.Observe("a"));
    }

    [Fact]
    public void The_mode_patch_covers_the_measured_phrase()
    {
        var patch = MiningModePatch.Of(2560, 1440);
        Assert.InRange(1191, patch.X, patch.X + patch.Width);
        Assert.InRange(1339, patch.X, patch.X + patch.Width);
        Assert.InRange(450, patch.Y, patch.Y + patch.Height);
        Assert.InRange(572, patch.Y, patch.Y + patch.Height);
    }
}
