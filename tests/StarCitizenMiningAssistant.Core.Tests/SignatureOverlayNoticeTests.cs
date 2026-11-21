using StarCitizenMiningAssistant.Core.Recognition;

namespace StarCitizenMiningAssistant.Core.Tests;

public class SignatureOverlayNoticeTests
{
    [Fact]
    public void A_different_line_is_a_new_result()
    {
        Assert.True(SignatureOverlayNotice.IsNewResult(null, "2x Stileron"));
        Assert.True(SignatureOverlayNotice.IsNewResult("2x Stileron", "3x Stileron"));
        Assert.Equal(TimeSpan.FromSeconds(10), SignatureOverlayNotice.VisibleFor);
    }

    [Fact]
    public void The_same_line_or_a_blank_is_not_a_new_result()
    {
        Assert.False(SignatureOverlayNotice.IsNewResult("2x Stileron", "2x Stileron"));
        Assert.False(SignatureOverlayNotice.IsNewResult("2x Stileron", "  2x Stileron  "));
        Assert.False(SignatureOverlayNotice.IsNewResult("2x Stileron", ""));
        Assert.False(SignatureOverlayNotice.IsNewResult("2x Stileron", null));
    }
}
