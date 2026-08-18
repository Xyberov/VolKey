using VolKey.Services;

namespace VolKey;

internal sealed class MainForm : Form
{
    private readonly Action _toggleEnabled;
    private readonly Action _saveSettings;
    private readonly Action _exitApplication;
    private readonly CheckBox _enabledCheckBox = new();
    private readonly CheckBox _autostartCheckBox = new();
    private readonly CheckBox _restoreVolumeCheckBox = new();
    private readonly NumericUpDown _timeoutInput = new();
    private readonly Label _statusLabel = new();
    private readonly NotifyIcon _trayIcon;
    private bool _allowClose;
    private bool _isInitializing = true;

    public AppSettings Settings { get; }

    public MainForm(AppSettings settings, bool autostartEnabled, Action toggleEnabled, Action saveSettings, Action exitApplication)
    {
        Settings = settings;
        Settings.StartWithWindows = autostartEnabled;
        _toggleEnabled = toggleEnabled;
        _saveSettings = saveSettings;
        _exitApplication = exitApplication;

        Text = "VolKey";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        ClientSize = new Size(470, 370);
        Font = new Font("Segoe UI", 10F);

        var heading = new Label { Text = "VolKey", Font = new Font("Segoe UI", 20F, FontStyle.Bold), AutoSize = true, Location = new Point(25, 20) };
        var description = new Label
        {
            Text = "Управление треками жестами на кнопках громкости.",
            AutoSize = true,
            Location = new Point(27, 60)
        };

        _enabledCheckBox.Text = "Включить обработку жестов";
        _enabledCheckBox.AutoSize = true;
        _enabledCheckBox.Location = new Point(28, 105);
        _enabledCheckBox.Checked = Settings.Enabled;
        _enabledCheckBox.CheckedChanged += (_, _) =>
        {
            if (!_isInitializing && _enabledCheckBox.Checked != Settings.Enabled)
                _toggleEnabled();
        };

        _statusLabel.AutoSize = true;
        _statusLabel.Location = new Point(48, 135);

        _autostartCheckBox.Text = "Запускать вместе с Windows";
        _autostartCheckBox.AutoSize = true;
        _autostartCheckBox.Location = new Point(28, 175);
        _autostartCheckBox.Checked = Settings.StartWithWindows;
        _autostartCheckBox.CheckedChanged += (_, _) =>
        {
            if (_isInitializing) return;
            Settings.StartWithWindows = _autostartCheckBox.Checked;
            _saveSettings();
        };

        _restoreVolumeCheckBox.Text = "Возвращать громкость после жеста";
        _restoreVolumeCheckBox.AutoSize = true;
        _restoreVolumeCheckBox.Location = new Point(28, 210);
        _restoreVolumeCheckBox.Checked = Settings.RestoreVolumeAfterGesture;
        _restoreVolumeCheckBox.CheckedChanged += (_, _) =>
        {
            if (_isInitializing) return;
            Settings.RestoreVolumeAfterGesture = _restoreVolumeCheckBox.Checked;
            _saveSettings();
        };

        var timeoutLabel = new Label { Text = "Пауза между нажатиями (мс):", AutoSize = true, Location = new Point(28, 250) };
        _timeoutInput.Minimum = 250;
        _timeoutInput.Maximum = 1500;
        _timeoutInput.Increment = 50;
        _timeoutInput.Value = Settings.GestureTimeoutMs;
        _timeoutInput.Location = new Point(270, 245);
        _timeoutInput.Width = 90;
        _timeoutInput.ValueChanged += (_, _) =>
        {
            Settings.GestureTimeoutMs = (int)_timeoutInput.Value;
            _saveSettings();
        };

        var help = new Label
        {
            Text = "+  −  +  — следующий трек\n−  +  −  — предыдущий трек",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Location = new Point(28, 290)
        };
        var exitButton = new Button { Text = "Выйти из программы", Size = new Size(155, 34), Location = new Point(285, 315) };
        exitButton.Click += (_, _) => ExitFromButton();

        Controls.AddRange([heading, description, _enabledCheckBox, _statusLabel, _autostartCheckBox, _restoreVolumeCheckBox, timeoutLabel, _timeoutInput, help, exitButton]);

        var menu = new ContextMenuStrip();
        var toggleItem = new ToolStripMenuItem("Включить / выключить", null, (_, _) => _toggleEnabled());
        var openItem = new ToolStripMenuItem("Открыть настройки", null, (_, _) => ShowWindow());
        var exitItem = new ToolStripMenuItem("Выйти", null, (_, _) => ExitFromButton());
        menu.Items.AddRange([toggleItem, openItem, new ToolStripSeparator(), exitItem]);

        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "VolKey",
            ContextMenuStrip = menu,
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => ShowWindow();

        FormClosing += OnFormClosing;
        _isInitializing = false;
        RefreshState();
    }

    public void RefreshState()
    {
        _enabledCheckBox.Checked = Settings.Enabled;
        _statusLabel.Text = Settings.Enabled ? "● Жесты включены" : "● Жесты выключены";
        _statusLabel.ForeColor = Settings.Enabled ? Color.ForestGreen : Color.Firebrick;
        _trayIcon.Text = Settings.Enabled ? "VolKey — жесты включены" : "VolKey — жесты выключены";
    }

    public void ShowNotification(string message)
    {
        _trayIcon.ShowBalloonTip(800, "VolKey", message, ToolTipIcon.Info);
    }

    private void ShowWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = true;
        Hide();
        _trayIcon.ShowBalloonTip(1200, "VolKey", "Программа продолжает работать возле часов.", ToolTipIcon.Info);
    }

    private void ExitFromButton()
    {
        _allowClose = true;
        _trayIcon.Visible = false;
        _exitApplication();
    }
}
