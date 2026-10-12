using VolKey.Services;

namespace VolKey;

internal sealed class MainForm : Form
{
    private readonly Action _toggleEnabled;
    private readonly Action _applySettings;
    private readonly Action _exitApplication;
    private readonly CheckBox _enabledCheckBox = new() { Text = "Включить обработку жестов", AutoSize = true };
    private readonly CheckBox _autostartCheckBox = new() { Text = "Запускать вместе с Windows", AutoSize = true };
    private readonly CheckBox _restoreVolumeCheckBox = new() { Text = "Возвращать громкость после жеста", AutoSize = true };
    private readonly CheckBox _notificationsCheckBox = new() { Text = "Показывать уведомление о переключении", AutoSize = true };
    private readonly NumericUpDown _timeoutInput = new();
    private readonly Label _statusLabel = new() { AutoSize = true };
    private readonly ToolStripMenuItem _toggleMenuItem;
    private readonly NotifyIcon _trayIcon;
    private readonly Icon _enabledTrayIcon = AppIcons.Load(SystemInformation.SmallIconSize);
    private readonly Icon _disabledTrayIcon;
    private bool _allowClose;
    private bool _isInitializing = true;
    private bool _hiddenHintShown;

    public AppSettings Settings { get; }

    public MainForm(AppSettings settings, Action toggleEnabled, Action applySettings, Action exitApplication)
    {
        Settings = settings;
        _toggleEnabled = toggleEnabled;
        _applySettings = applySettings;
        _exitApplication = exitApplication;
        _disabledTrayIcon = AppIcons.CreateDisabled(_enabledTrayIcon);

        Text = "VolKey";
        Icon = AppIcons.Load(SystemInformation.IconSize);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 10F);
        Padding = new Padding(Dpi(22), Dpi(14), Dpi(22), Dpi(18));

        var layout = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        var version = typeof(MainForm).Assembly.GetName().Version?.ToString(3);
        var titleRow = Row(
            new Label { Text = "VolKey", Font = new Font(Font.FontFamily, 20F, FontStyle.Bold), AutoSize = true, Margin = Padding.Empty },
            new Label { Text = $"v{version}", ForeColor = SystemColors.GrayText, AutoSize = true, Margin = new Padding(Dpi(4), Dpi(16), 0, 0) });
        var description = new Label
        {
            Text = "Переключение треков жестами на кнопках громкости.",
            AutoSize = true,
            Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(8))
        };
        _statusLabel.Font = new Font(Font, FontStyle.Bold);
        _statusLabel.Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(10));

        foreach (var checkBox in new[] { _enabledCheckBox, _autostartCheckBox, _restoreVolumeCheckBox, _notificationsCheckBox })
            checkBox.Margin = new Padding(Dpi(3), Dpi(3), Dpi(3), Dpi(5));
        _enabledCheckBox.Checked = Settings.Enabled;
        _enabledCheckBox.CheckedChanged += (_, _) =>
        {
            if (!_isInitializing && _enabledCheckBox.Checked != Settings.Enabled)
                _toggleEnabled();
        };
        BindCheckBox(_autostartCheckBox, Settings.StartWithWindows, value => Settings.StartWithWindows = value);
        BindCheckBox(_restoreVolumeCheckBox, Settings.RestoreVolumeAfterGesture, value => Settings.RestoreVolumeAfterGesture = value);
        BindCheckBox(_notificationsCheckBox, Settings.ShowNotifications, value => Settings.ShowNotifications = value);

        _timeoutInput.Minimum = AppSettings.MinGestureTimeoutMs;
        _timeoutInput.Maximum = AppSettings.MaxGestureTimeoutMs;
        _timeoutInput.Increment = 50;
        _timeoutInput.Value = Settings.GestureTimeoutMs;
        _timeoutInput.Width = Dpi(80);
        _timeoutInput.ValueChanged += (_, _) =>
        {
            if (_isInitializing) return;
            Settings.GestureTimeoutMs = (int)_timeoutInput.Value;
            _applySettings();
        };
        var timeoutRow = Row(
            new Label { Text = "Макс. пауза между нажатиями, мс:", AutoSize = true, Margin = new Padding(Dpi(3), Dpi(6), Dpi(6), 0) },
            _timeoutInput);
        timeoutRow.Margin = new Padding(0, Dpi(4), 0, Dpi(12));

        var gestures = new Label
        {
            Text = "Громкость  +  −  +   →   следующий трек\nГромкость  −  +  −   →   предыдущий трек",
            ForeColor = SystemColors.GrayText,
            AutoSize = true,
            Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(14))
        };

        var exitButton = new Button { Text = "Выйти из программы", AutoSize = true, Padding = new Padding(Dpi(6), Dpi(2), Dpi(6), Dpi(2)) };
        exitButton.Click += (_, _) => ExitFromButton();
        var hideButton = new Button { Text = "Свернуть в трей", AutoSize = true, Padding = new Padding(Dpi(6), Dpi(2), Dpi(6), Dpi(2)) };
        hideButton.Click += (_, _) => Close();
        var buttonsRow = Row(exitButton, hideButton);
        buttonsRow.FlowDirection = FlowDirection.RightToLeft;
        buttonsRow.Anchor = AnchorStyles.Right;

        layout.Controls.AddRange([titleRow, description, _statusLabel, _enabledCheckBox, _autostartCheckBox,
            _restoreVolumeCheckBox, _notificationsCheckBox, timeoutRow, gestures, buttonsRow]);
        Controls.Add(layout);
        CancelButton = hideButton;

        var openItem = new ToolStripMenuItem("Открыть VolKey", null, (_, _) => ShowWindow()) { Font = new Font(Font, FontStyle.Bold) };
        _toggleMenuItem = new ToolStripMenuItem("Жесты включены", null, (_, _) => _toggleEnabled());
        var exitItem = new ToolStripMenuItem("Выйти", null, (_, _) => ExitFromButton());
        var menu = new ContextMenuStrip();
        menu.Items.AddRange([openItem, _toggleMenuItem, new ToolStripSeparator(), exitItem]);

        _trayIcon = new NotifyIcon
        {
            Icon = _enabledTrayIcon,
            Text = "VolKey",
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ShowWindow();
        };

        _isInitializing = false;
        RefreshState();
    }

    public void RefreshState()
    {
        _enabledCheckBox.Checked = Settings.Enabled;
        _statusLabel.Text = Settings.Enabled ? "● Жесты включены" : "● Жесты выключены";
        _statusLabel.ForeColor = Settings.Enabled ? Color.ForestGreen : Color.Firebrick;
        _toggleMenuItem.Checked = Settings.Enabled;
        _trayIcon.Icon = Settings.Enabled ? _enabledTrayIcon : _disabledTrayIcon;
        _trayIcon.Text = Settings.Enabled ? "VolKey — жесты включены" : "VolKey — жесты выключены";
    }

    public void ShowNotification(string message)
    {
        _trayIcon.ShowBalloonTip(800, "VolKey", message, ToolTipIcon.None);
    }

    public void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState == FormWindowState.Minimized) HideToTray();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        // Крестик прячет окно в трей. Выход из системы и прочие причины закрытия не блокируем.
        if (_allowClose || e.CloseReason != CloseReason.UserClosing) return;
        e.Cancel = true;
        HideToTray();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _enabledTrayIcon.Dispose();
            _disabledTrayIcon.Dispose();
        }
        base.Dispose(disposing);
    }

    private void HideToTray()
    {
        Hide();
        if (_hiddenHintShown) return;
        _hiddenHintShown = true;
        _trayIcon.ShowBalloonTip(1200, "VolKey", "Программа продолжает работать возле часов.", ToolTipIcon.Info);
    }

    private void BindCheckBox(CheckBox checkBox, bool value, Action<bool> setValue)
    {
        checkBox.Checked = value;
        checkBox.CheckedChanged += (_, _) =>
        {
            if (_isInitializing) return;
            setValue(checkBox.Checked);
            _applySettings();
        };
    }

    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
        row.Controls.AddRange(controls);
        return row;
    }

    private int Dpi(int value) => LogicalToDeviceUnits(value);

    private void ExitFromButton()
    {
        _allowClose = true;
        _exitApplication();
    }
}
