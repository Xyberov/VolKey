using Microsoft.Win32;

namespace VolKey.Services;

internal sealed class AutoStartService
{
    public const string MinimizedArgument = "--minimized";
    private const string AppName = "VolKey";
    private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

    private static string QuotedExecutablePath => $"\"{Application.ExecutablePath}\"";
    private static string Command => $"{QuotedExecutablePath} {MinimizedArgument}";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName) is not null;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (enabled)
            key.SetValue(AppName, Command);
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }

    /// <summary>Дописывает <see cref="MinimizedArgument"/> к записи, оставшейся от старой версии.</summary>
    public void UpgradeCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key?.GetValue(AppName) is string value && value != Command &&
            value.StartsWith(QuotedExecutablePath, StringComparison.OrdinalIgnoreCase))
            key.SetValue(AppName, Command);
    }
}
