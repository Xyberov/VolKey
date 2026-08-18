using Microsoft.Win32;

namespace VolKey.Services;

internal sealed class AutoStartService
{
    private const string AppName = "VolKey";
    private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

    public bool IsEnabled() => Registry.CurrentUser.OpenSubKey(RunKey)?.GetValue(AppName) is not null;

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)!;
        if (enabled)
            key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
