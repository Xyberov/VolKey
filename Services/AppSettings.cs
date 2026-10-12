namespace VolKey.Services;

internal enum UiLanguage { System, Russian, English }

internal enum UiTheme { System, Light, Dark }

internal sealed class AppSettings
{
    public const int MinGestureTimeoutMs = 250;
    public const int MaxGestureTimeoutMs = 1500;

    public bool Enabled { get; set; } = true;
    public bool StartWithWindows { get; set; }
    public bool RestoreVolumeAfterGesture { get; set; } = true;
    /// <summary>Показывать плашку с действием и треком (имя свойства сохранено ради старых файлов настроек).</summary>
    public bool ShowNotifications { get; set; } = true;
    public bool IgnoreInFullscreen { get; set; } = true;
    public int GestureTimeoutMs { get; set; } = 550;
    public UiLanguage Language { get; set; }
    public UiTheme Theme { get; set; }
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
        if (!Enum.IsDefined(Language)) Language = UiLanguage.System;
        if (!Enum.IsDefined(Theme)) Theme = UiTheme.System;
        Gestures = (Gestures ?? DefaultGestures())
            .Where(gesture => gesture is not null && GestureSequence.IsValid(gesture.Sequence) && Enum.IsDefined(gesture.Action))
            .DistinctBy(gesture => gesture.Sequence)
            .ToList();
        return this;
    }
}
