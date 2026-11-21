using System.Runtime.InteropServices;
using StarCitizenMiningAssistant.Core.Input;

namespace StarCitizenMiningAssistant.App;

internal static class GlobalHotkey
{
    public const int Message = 0x0312;

    public static bool Register(IntPtr handle, int id, HotkeyChord chord) =>
        RegisterHotKey(handle, id, (uint)(chord.Modifiers | 0x4000), (uint)chord.VirtualKey);

    public static void Unregister(IntPtr handle, int id) => UnregisterHotKey(handle, id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr handle, int id, uint modifiers, uint virtualKey);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr handle, int id);
}
