using System.Globalization;
using VolKey.Services;

namespace VolKey;

/// <summary>Тексты интерфейса на русском и английском.</summary>
internal static class Strings
{
    private static bool _russian = true;

    public static void SetLanguage(UiLanguage language) => _russian = language switch
    {
        UiLanguage.Russian => true,
        UiLanguage.English => false,
        _ => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru"
    };

    private static string T(string russian, string english) => _russian ? russian : english;

    public static string Description => T("Управление музыкой жестами на кнопках громкости.", "Control your music with gestures on the volume keys.");
    public static string Status(bool enabled) => enabled ? T("● Жесты включены", "● Gestures are on") : T("● Жесты выключены", "● Gestures are off");
    public static string EnableGestures => T("Включить обработку жестов", "Enable gestures");
    public static string StartWithWindows => T("Запускать вместе с Windows", "Start with Windows");
    public static string RestoreVolume => T("Возвращать громкость после жеста", "Restore volume after a gesture");
    public static string ShowNotifications => T("Показывать уведомление о действии", "Show a notification for each action");
    public static string MaxPause => T("Макс. пауза между нажатиями, мс:", "Max pause between presses, ms:");
    public static string Language => T("Язык:", "Language:");

    public static string LanguageName(UiLanguage language) => language switch
    {
        UiLanguage.Russian => "Русский",
        UiLanguage.English => "English",
        _ => T("Как в системе", "System default")
    };

    public static string Gestures => T("Жесты", "Gestures");
    public static string NoGestures => T("Жестов нет — добавьте первый.", "No gestures yet — add one.");
    public static string AddGesture => T("Добавить жест", "Add gesture");
    public static string RecordGesture => T("Записать жест", "Record gesture");
    public static string ResetGestures => T("Вернуть стандартные", "Restore defaults");
    public static string RemoveGesture => T("Удалить жест", "Remove gesture");
    public static string RecordSequence => T("Записать комбинацию кнопками громкости", "Record with the volume keys");
    public static string StopRecording => T("Отменить запись", "Cancel recording");
    public static string RecordingPrompt => T("Нажимайте кнопки громкости… Esc — отмена.", "Press the volume keys… Esc to cancel.");
    public static string Recorded(string sequence) => T($"Записано: {sequence}", $"Recorded: {sequence}");
    public static string InvalidRecording => T("Нужно от 2 до 4 нажатий и обе кнопки — например, + − +.", "Use 2–4 presses with both keys, e.g. + − +.");
    public static string AlreadyAssigned(string sequence) => T($"Комбинация «{sequence}» уже назначена.", $"“{sequence}” is already assigned.");
    public static string PrefixHint => T("Комбинация, с которой начинается другая, срабатывает после паузы.", "A combination that starts a longer one fires after a pause.");
    public static string HideToTray => T("Свернуть в трей", "Hide to tray");
    public static string Quit => T("Выйти из программы", "Quit VolKey");

    public static string TrayOpen => T("Открыть VolKey", "Open VolKey");
    public static string TrayGesturesOn => T("Жесты включены", "Gestures on");
    public static string TrayQuit => T("Выйти", "Quit");
    public static string TrayTooltip(bool enabled) => enabled ? T("VolKey — жесты включены", "VolKey — gestures on") : T("VolKey — жесты выключены", "VolKey — gestures off");
    public static string StillRunning => T("Программа продолжает работать возле часов.", "VolKey keeps running in the system tray.");

    public static string Action(GestureAction action) => action switch
    {
        GestureAction.NextTrack => T("Следующий трек", "Next track"),
        GestureAction.PreviousTrack => T("Предыдущий трек", "Previous track"),
        GestureAction.PlayPause => T("Пауза / воспроизведение", "Play / pause"),
        GestureAction.ToggleMute => T("Выключить / включить звук", "Mute / unmute"),
        _ => action.ToString()
    };

    public static string Muted(bool muted) => muted ? T("Звук выключен", "Sound muted") : T("Звук включён", "Sound on");
}
