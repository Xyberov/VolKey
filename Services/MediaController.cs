using System.Runtime.InteropServices;

namespace VolKey.Services;

internal static class MediaController
{
    private const byte VkMediaNextTrack = 0xB0;
    private const byte VkMediaPrevTrack = 0xB1;
    private const byte VkMediaPlayPause = 0xB3;
    private const uint KeyeventfKeyUp = 0x0002;

    /// <summary>Отправляет медиаклавишу для действий, которые выполняет плеер. Возвращает false для остальных.</summary>
    public static bool TrySend(GestureAction action)
    {
        byte? key = action switch
        {
            GestureAction.NextTrack => VkMediaNextTrack,
            GestureAction.PreviousTrack => VkMediaPrevTrack,
            GestureAction.PlayPause => VkMediaPlayPause,
            _ => null
        };
        if (key is null) return false;
        keybd_event(key.Value, 0, 0, UIntPtr.Zero);
        keybd_event(key.Value, 0, KeyeventfKeyUp, UIntPtr.Zero);
        return true;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);
}
