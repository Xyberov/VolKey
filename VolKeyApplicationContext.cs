using VolKey.Services;

namespace VolKey;

internal sealed class VolKeyApplicationContext : ApplicationContext
{
    private readonly SettingsService _settingsService = new();
    private readonly AutoStartService _autoStartService = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly AudioVolumeService _audioVolumeService = new();
    private readonly MediaSessionService _mediaSessionService = new();
    private readonly OsdWindow _osd = new();
    private readonly GestureRecognizer _gestureRecognizer;
    private readonly GestureRecorder _gestureRecorder = new();
    private readonly MainForm _mainForm;
    private readonly SynchronizationContext _uiContext;
    private readonly ShowWindowSignal _showWindowSignal;
    private int _osdVersion;

    public VolKeyApplicationContext(bool startMinimized)
    {
        var settings = _settingsService.Load();
        settings.StartWithWindows = _autoStartService.IsEnabled();
        _autoStartService.UpgradeCommand();
        Strings.SetLanguage(settings.Language);

        _gestureRecorder.Recorded += (_, e) => RestoreAfterPresses(e.VolumeBefore?.Level, e.VolumeBefore?.Muted);
        _mainForm = new MainForm(settings, _gestureRecorder, _mediaSessionService, ToggleEnabled, ApplySettings, ExitApplication);
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        _gestureRecognizer = new GestureRecognizer(settings.GestureTimeoutMs, settings.Gestures);
        _gestureRecognizer.GestureRecognized += OnGestureRecognized;
        _keyboardHook.VolumeKeyPressed += OnVolumeKeyPressed;
        _keyboardHook.Start();
        // Первое обращение к медиасессиям медленное — делаем его заранее, а не во время жеста.
        _ = _mediaSessionService.GetCurrentTrackAsync();

        _showWindowSignal = new ShowWindowSignal(() => _uiContext.Post(_ => _mainForm.ShowWindow(), null));
        if (!startMinimized) _mainForm.ShowWindow();
    }

    private void OnVolumeKeyPressed(object? sender, VolumeKey key)
    {
        var recording = _gestureRecorder.IsRecording;
        var settings = _mainForm.Settings;
        if (!recording && (!settings.Enabled || settings.IgnoreInFullscreen && FullscreenDetector.IsFullscreenAppActive()))
            return;

        // Хук получает кнопку раньше, чем Windows изменяет громкость, поэтому уровень читаем здесь.
        // Всё остальное (отправку медиаклавиш, уведомления) выполняем уже после выхода из хука.
        VolumeState? volumeBefore = _audioVolumeService.TryGetState(out var state) ? state : null;
        if (recording)
            _uiContext.Post(_ => _gestureRecorder.Register(key, volumeBefore), null);
        else
            _uiContext.Post(_ => _gestureRecognizer.Register(key, volumeBefore), null);
    }

    private async void OnGestureRecognized(object? sender, GestureRecognizedEventArgs e)
    {
        var settings = _mainForm.Settings;
        // Трек до команды нужен, чтобы плашка дождалась, когда плеер его сменит.
        var trackBefore = settings.ShowNotifications && e.Action != GestureAction.ToggleMute
            ? await _mediaSessionService.GetCurrentTrackAsync()
            : null;
        MediaController.TrySend(e.Action);

        var restore = settings.RestoreVolumeAfterGesture;
        float? level = restore ? e.VolumeBefore?.Level : null;
        bool? muted = restore ? e.VolumeBefore?.Muted : null;
        if (e.Action == GestureAction.ToggleMute)
        {
            // Сами нажатия «Громкость +» снимают режим без звука, поэтому отталкиваемся от состояния до жеста.
            var before = e.VolumeBefore ?? (_audioVolumeService.TryGetState(out var current) ? current : null);
            muted = before.HasValue ? !before.Value.Muted : null;
        }
        RestoreAfterPresses(level, muted);

        if (settings.ShowNotifications)
            await ShowOsdAsync(e.Action, trackBefore, muted);
    }

    private async Task ShowOsdAsync(GestureAction action, TrackInfo? before, bool? muted)
    {
        // Если за время ожидания плеера сработал следующий жест, старый результат не показываем.
        var version = ++_osdVersion;
        switch (action)
        {
            case GestureAction.ToggleMute:
                _osd.ShowMessage(muted == true ? OsdGlyph.Muted : OsdGlyph.Volume,
                    muted.HasValue ? Strings.Muted(muted.Value) : Strings.Action(action));
                return;

            case GestureAction.PlayPause:
            {
                bool? willPlay = before is null ? null : !before.IsPlaying;
                _osd.ShowMessage(PlaybackGlyph(willPlay), willPlay.HasValue ? Strings.Playback(willPlay.Value) : Strings.Action(action),
                    before?.Display);
                var after = await _mediaSessionService.WaitForChangeAsync(
                    track => before is null || track.IsPlaying != before.IsPlaying, TimeSpan.FromSeconds(1));
                if (after is not null && version == _osdVersion)
                    _osd.ShowMessage(PlaybackGlyph(after.IsPlaying), Strings.Playback(after.IsPlaying), after.Display);
                return;
            }

            default:
            {
                var glyph = action == GestureAction.NextTrack ? OsdGlyph.Next : OsdGlyph.Previous;
                _osd.ShowMessage(glyph, Strings.Action(action));
                var after = await _mediaSessionService.WaitForChangeAsync(
                    track => before is null || !track.IsSameTrack(before), TimeSpan.FromSeconds(1.5));
                if (after is null || version != _osdVersion) return;
                var cover = await _mediaSessionService.GetCoverAsync();
                if (version != _osdVersion)
                {
                    cover?.Dispose();
                    return;
                }
                _osd.ShowMessage(glyph, after.Title, string.IsNullOrEmpty(after.Artist) ? Strings.Action(action) : after.Artist, cover);
                return;
            }
        }
    }

    private static string PlaybackGlyph(bool? playing) => playing == false ? OsdGlyph.Pause : OsdGlyph.Play;

    private void RestoreAfterPresses(float? level, bool? muted)
    {
        if (!level.HasValue && !muted.HasValue) return;
        // Даём системе закончить обработку нажатий и возвращаем уровень.
        // Повтор страхует от случаев, когда Windows применила последнее нажатие с опозданием.
        RunAfter(80, () => _audioVolumeService.Apply(level, muted));
        RunAfter(250, () => _audioVolumeService.Apply(level, muted));
    }

    private void ToggleEnabled()
    {
        _mainForm.Settings.Enabled = !_mainForm.Settings.Enabled;
        ApplySettings();
        _mainForm.RefreshState();
    }

    private void ApplySettings()
    {
        var settings = _mainForm.Settings;
        _settingsService.Save(settings);
        if (settings.StartWithWindows != _autoStartService.IsEnabled())
            _autoStartService.SetEnabled(settings.StartWithWindows);
        _gestureRecognizer.TimeoutMs = settings.GestureTimeoutMs;
        _gestureRecognizer.SetGestures(settings.Gestures);
    }

    private static void RunAfter(int delayMs, Action action)
    {
        var timer = new System.Windows.Forms.Timer { Interval = delayMs };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            action();
        };
        timer.Start();
    }

    private void ExitApplication()
    {
        _keyboardHook.Dispose();
        _showWindowSignal.Dispose();
        _gestureRecognizer.Dispose();
        _gestureRecorder.Dispose();
        _audioVolumeService.Dispose();
        _mainForm.Dispose();
        _osd.Dispose();
        ExitThread();
    }
}
