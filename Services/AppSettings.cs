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
    public List<GestureBinding> Gestures { get; set; } = DefaultGestures();

    public static List<GestureBinding> DefaultGestures() =>
    [
        new("+-+", GestureAction.NextTrack),
        new("-+-", GestureAction.PreviousTrack)
    ];

    /// <summary>Приводит значения из файла (возможно, отредактированного вручную) к допустимым.</summary>
    public AppSettings Normalize()
    {
        GestureTimeoutMs = Math.Clamp(GestureTimeoutMs, MinGestureTimeoutMs, MaxGestureTimeoutMs);
        Gestures = (Gestures ?? DefaultGestures())
            .Where(gesture => gesture is not null && GestureSequence.IsValid(gesture.Sequence) && Enum.IsDefined(gesture.Action))
            .DistinctBy(gesture => gesture.Sequence)
            .ToList();
        return this;
    }
}
