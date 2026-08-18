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
    private float? _volumeBeforeGesture;

    public VolKeyApplicationContext()
    {
        var settings = _settingsService.Load();
        _gestureRecognizer = new GestureRecognizer(settings.GestureTimeoutMs);
        _gestureRecognizer.GestureRecognized += OnGestureRecognized;
        _gestureRecognizer.GestureFinished += (_, _) => _volumeBeforeGesture = null;
        _keyboardHook.VolumeKeyPressed += OnVolumeKeyPressed;
        _keyboardHook.Start();

        _mainForm = new MainForm(settings, _autoStartService.IsEnabled(), ToggleEnabled, SaveSettings, ExitApplication);
        _mainForm.Show();
    }

    private void OnVolumeKeyPressed(object? sender, VolumeKey key)
    {
        if (_mainForm.Settings.Enabled)
        {
            if (_volumeBeforeGesture is null && _mainForm.Settings.RestoreVolumeAfterGesture &&
                _audioVolumeService.TryGetMasterVolume(out var volume))
                _volumeBeforeGesture = volume;
            _gestureRecognizer.Register(key);
        }
    }

    private void OnGestureRecognized(object? sender, MediaCommand command)
    {
        var volumeToRestore = _volumeBeforeGesture;
        MediaController.Send(command);
        if (volumeToRestore.HasValue)
        {
            // Хук получает кнопку раньше, чем Windows изменяет громкость. Небольшая задержка
            // даёт системе закончить обработку всех трёх нажатий, затем возвращает уровень.
            var restoreTimer = new System.Windows.Forms.Timer { Interval = 80 };
            restoreTimer.Tick += (_, _) =>
            {
                restoreTimer.Stop();
                restoreTimer.Dispose();
                _audioVolumeService.RestoreMasterVolume(volumeToRestore.Value);
            };
            restoreTimer.Start();
        }
        _mainForm.ShowNotification(command == MediaCommand.Next ? "Следующий трек" : "Предыдущий трек");
    }

    private void ToggleEnabled()
    {
        _mainForm.Settings.Enabled = !_mainForm.Settings.Enabled;
        SaveSettings();
        _mainForm.RefreshState();
    }

    private void SaveSettings()
    {
        _settingsService.Save(_mainForm.Settings);
        _autoStartService.SetEnabled(_mainForm.Settings.StartWithWindows);
        _gestureRecognizer.TimeoutMs = _mainForm.Settings.GestureTimeoutMs;
    }

    private void ExitApplication()
    {
        _keyboardHook.Dispose();
        _gestureRecognizer.Dispose();
        _mainForm.Dispose();
        ExitThread();
    }
}
