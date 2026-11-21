namespace StarCitizenMiningAssistant.Core.Input;

/// <summary>
/// A keyboard shortcut stored as text, such as F8 or Ctrl+Alt+F8.
/// Modifiers match RegisterHotKey: Alt 1, Control 2, Shift 4, Win 8.
/// </summary>
public readonly record struct HotkeyChord(int Modifiers, int VirtualKey, string Text)
{
    public const int Alt = 1;
    public const int Control = 2;
    public const int Shift = 4;
    public const int Win = 8;

    public static HotkeyChord? Parse(string? text)
    {
        var raw = text?.Trim() ?? "";
        if (raw.Length == 0)
        {
            return null;
        }

        var parts = raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        var modifiers = 0;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            modifiers |= parts[i].ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => Control,
                "ALT" => Alt,
                "SHIFT" => Shift,
                "WIN" or "WINDOWS" => Win,
                _ => -1
            };
            if (modifiers < 0)
            {
                return null;
            }
        }

        if (!TryKey(parts[^1], out var virtualKey))
        {
            return null;
        }

        return new HotkeyChord(modifiers, virtualKey, Format(modifiers, parts[^1].ToUpperInvariant()));
    }

    public static string Format(int modifiers, string key)
    {
        var parts = new List<string>();
        if ((modifiers & Control) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & Alt) != 0)
        {
            parts.Add("Alt");
        }

        if ((modifiers & Shift) != 0)
        {
            parts.Add("Shift");
        }

        if ((modifiers & Win) != 0)
        {
            parts.Add("Win");
        }

        parts.Add(key);
        return string.Join('+', parts);
    }

    private static bool TryKey(string token, out int virtualKey)
    {
        var key = token.ToUpperInvariant();
        if (key.Length == 1 && key[0] is >= 'A' and <= 'Z' or >= '0' and <= '9')
        {
            virtualKey = key[0];
            return true;
        }

        if (key.Length is 2 or 3 && key[0] == 'D' && int.TryParse(key[1..], out var digit) && digit is >= 0 and <= 9)
        {
            virtualKey = '0' + digit;
            return true;
        }

        if (key.Length is >= 2 and <= 3 && key[0] == 'F' && int.TryParse(key[1..], out var function) && function is >= 1 and <= 24)
        {
            virtualKey = 0x70 + function - 1;
            return true;
        }

        if (key.StartsWith("NUMPAD", StringComparison.Ordinal) && int.TryParse(key[6..], out var pad) && pad is >= 0 and <= 9)
        {
            virtualKey = 0x60 + pad;
            return true;
        }

        switch (key)
        {
            case "SPACE":
                virtualKey = 0x20;
                return true;
            case "TAB":
                virtualKey = 0x09;
                return true;
            case "ENTER" or "RETURN":
                virtualKey = 0x0D;
                return true;
            default:
                virtualKey = 0;
                return false;
        }
    }
}
