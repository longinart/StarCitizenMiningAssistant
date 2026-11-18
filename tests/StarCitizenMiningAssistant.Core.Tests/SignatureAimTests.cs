using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class SignatureAimTests
{
    [Fact]
    public void A_miss_checks_the_center_then_one_step_each_way()
    {
        var aim = new SignatureAim();
        Assert.Equal(100, ScanPatch.Step(1440));
        Assert.Equal(
            [(0, 0), (0, -100), (0, 100), (-100, 0), (100, 0)],
            aim.Order(1440));
    }

    [Fact]
    public void The_shift_that_worked_is_tried_first_and_is_not_listed_twice()
    {
        var aim = new SignatureAim();
        aim.Remember(0, -100);
        Assert.Equal(
            [(0, -100), (0, 0), (0, 100), (-100, 0), (100, 0)],
            aim.Order(1440));

        aim.Remember(0, 0);
        Assert.Equal(
            [(0, 0), (0, -100), (0, 100), (-100, 0), (100, 0)],
            aim.Order(1440));
    }

    [Fact]
    public void A_step_that_leaves_the_monitor_is_skipped()
    {
        Assert.Null(ScanPatch.Shifted(2560, 1440, -2000, 0));
        var center = ScanPatch.Of(2560, 1440);
        var shifted = ScanPatch.Shifted(2560, 1440, 0, -100);
        Assert.NotNull(shifted);
        Assert.Equal(center.X, shifted.Value.X);
        Assert.Equal(center.Y - 100, shifted.Value.Y);
        Assert.Equal(center.Width, shifted.Value.Width);
        Assert.Equal(center.Height, shifted.Value.Height);
    }
}
