using VolKey.Services;

namespace VolKey;

internal sealed class VolKeyApplicationContext : ApplicationContext
{
    private readonly SettingsService _settingsService = new();
    private readonly AutoStartService _autoStartService = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly AudioVolumeService _audioVolumeService = new();
    private readonly GestureRecognizer _gestureRecognizer;
    private readonly MainForm _mainForm;
    private readonly SynchronizationContext _uiContext;
    private readonly ShowWindowSignal _showWindowSignal;

    public VolKeyApplicationContext(bool startMinimized)
    {
        var settings = _settingsService.Load();
        settings.StartWithWindows = _autoStartService.IsEnabled();
        _autoStartService.UpgradeCommand();

        _mainForm = new MainForm(settings, ToggleEnabled, ApplySettings, ExitApplication);
        _uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

        _gestureRecognizer = new GestureRecognizer(settings.GestureTimeoutMs);
        _gestureRecognizer.GestureRecognized += OnGestureRecognized;
        _keyboardHook.VolumeKeyPressed += OnVolumeKeyPressed;
        _keyboardHook.Start();

        _showWindowSignal = new ShowWindowSignal(() => _uiContext.Post(_ => _mainForm.ShowWindow(), null));
        if (!startMinimized) _mainForm.ShowWindow();
    }

    private void OnVolumeKeyPressed(object? sender, VolumeKey key)
    {
        if (!_mainForm.Settings.Enabled) return;

        // Хук получает кнопку раньше, чем Windows изменяет громкость, поэтому уровень читаем здесь.
        // Всё остальное (отправку медиаклавиш, уведомления) выполняем уже после выхода из хука.
        VolumeState? volumeBefore = _mainForm.Settings.RestoreVolumeAfterGesture &&
            _audioVolumeService.TryGetState(out var state) ? state : null;
        _uiContext.Post(_ => _gestureRecognizer.Register(key, volumeBefore), null);
    }

    private void OnGestureRecognized(object? sender, GestureRecognizedEventArgs e)
    {
        MediaController.Send(e.Command);
        if (e.VolumeBefore is { } volume)
        {
            // Даём системе закончить обработку всех трёх нажатий и возвращаем уровень.
            // Повтор страхует от случаев, когда Windows применила последнее нажатие с опозданием.
            RunAfter(80, () => _audioVolumeService.Restore(volume));
            RunAfter(250, () => _audioVolumeService.Restore(volume));
        }
        if (_mainForm.Settings.ShowNotifications)
            _mainForm.ShowNotification(e.Command == MediaCommand.Next ? "Следующий трек" : "Предыдущий трек");
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
        if (!settings.Enabled) _gestureRecognizer.Reset();
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
        _audioVolumeService.Dispose();
        _mainForm.Dispose();
        ExitThread();
    }
}
