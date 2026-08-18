namespace VolKey.Services;

internal sealed class AppSettings
{
    public bool Enabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public int GestureTimeoutMs { get; set; } = 550;
}
