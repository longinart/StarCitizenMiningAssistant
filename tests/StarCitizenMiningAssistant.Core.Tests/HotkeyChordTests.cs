using StarCitizenMiningAssistant.Core.Input;

namespace StarCitizenMiningAssistant.Core.Tests;

public class HotkeyChordTests
{
    [Fact]
    public void A_function_key_and_modifiers_round_trip()
    {
        var chord = HotkeyChord.Parse("Ctrl+Alt+F8");

        Assert.NotNull(chord);
        Assert.Equal(HotkeyChord.Control | HotkeyChord.Alt, chord.Value.Modifiers);
        Assert.Equal(0x77, chord.Value.VirtualKey);
        Assert.Equal("Ctrl+Alt+F8", chord.Value.Text);
    }

    [Fact]
    public void The_same_text_is_one_chord()
    {
        var rock = HotkeyChord.Parse("f8");
        var signature = HotkeyChord.Parse("F8");

        Assert.Equal(rock, signature);
    }

    [Fact]
    public void A_modifier_alone_or_a_blank_is_not_a_chord()
    {
        Assert.Null(HotkeyChord.Parse(""));
        Assert.Null(HotkeyChord.Parse("Ctrl"));
        Assert.Null(HotkeyChord.Parse("Ctrl+Nope"));
    }
}
