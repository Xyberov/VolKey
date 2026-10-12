namespace VolKey.Services;

internal sealed class AppSettings
{
    public const int MinGestureTimeoutMs = 250;
    public const int MaxGestureTimeoutMs = 1500;

    public bool Enabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool RestoreVolumeAfterGesture { get; set; } = true;
    public bool ShowNotifications { get; set; } = true;
    public int GestureTimeoutMs { get; set; } = 550;

    /// <summary>Приводит значения из файла (возможно, отредактированного вручную) к допустимым.</summary>
    public AppSettings Normalize()
    {
        GestureTimeoutMs = Math.Clamp(GestureTimeoutMs, MinGestureTimeoutMs, MaxGestureTimeoutMs);
        return this;
    }
}
