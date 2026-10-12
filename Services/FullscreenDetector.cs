using System.Runtime.InteropServices;

namespace VolKey.Services;

internal static class FullscreenDetector
{
    private const int QunsBusy = 2;
    private const int QunsRunningD3DFullScreen = 3;
    private const int QunsPresentationMode = 4;

    /// <summary>
    /// Открыто ли полноэкранное приложение: игра (в том числе в окне без рамки), видео во весь экран
    /// или презентация. Это та же проверка, по которой Windows приглушает уведомления.
    /// </summary>
    public static bool IsFullscreenAppActive() =>
        SHQueryUserNotificationState(out var state) == 0 &&
        state is QunsBusy or QunsRunningD3DFullScreen or QunsPresentationMode;

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out int state);
}
