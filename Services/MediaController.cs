using System.Runtime.InteropServices;

namespace VolKey.Services;

internal static class MediaController
{
    private const byte VkMediaNextTrack = 0xB0;
    private const byte VkMediaPrevTrack = 0xB1;
    private const uint KeyeventfKeyUp = 0x0002;

    public static void Send(MediaCommand command)
    {
        var key = command == MediaCommand.Next ? VkMediaNextTrack : VkMediaPrevTrack;
        keybd_event(key, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, KeyeventfKeyUp, UIntPtr.Zero);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
}
