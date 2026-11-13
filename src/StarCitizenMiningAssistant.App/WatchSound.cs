using System.Runtime.InteropServices;

namespace StarCitizenMiningAssistant.App;

internal static class WatchSound
{
    public static void Play() => PlaySound("SystemAsterisk", IntPtr.Zero, 0x00010000);

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern bool PlaySound(string name, IntPtr module, uint flags);
}
