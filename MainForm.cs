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
    private readonly CheckBox _notificationsCheckBox = new() { Text = "Показывать уведомление о действии", AutoSize = true };
    private readonly NumericUpDown _timeoutInput = new();
    private readonly Label _statusLabel = new() { AutoSize = true };
    private readonly TableLayoutPanel _gesturesTable = new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 4 };
    private readonly Button _addGestureButton = new() { Text = "Добавить жест", AutoSize = true };
    private readonly Label _gestureMessage = new() { AutoSize = true, ForeColor = Color.Firebrick, Visible = false };
    private readonly Label _gestureHint = new()
    {
        Text = "Комбинация, с которой начинается другая, срабатывает после паузы.",
        AutoSize = true,
        ForeColor = SystemColors.GrayText,
        Visible = false
    };
    private readonly ToolStripMenuItem _toggleMenuItem;
    private readonly NotifyIcon _trayIcon;
    private readonly ToolTip _toolTip = new();
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
            Text = "Управление музыкой жестами на кнопках громкости.",
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

        var gesturesHeading = new Label { Text = "Жесты", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(4)) };
        _gesturesTable.Margin = Padding.Empty;
        _addGestureButton.Click += (_, _) => AddGesture();
        var resetGesturesLink = new LinkLabel { Text = "Вернуть стандартные", AutoSize = true, Margin = new Padding(Dpi(10), Dpi(8), 0, 0) };
        resetGesturesLink.LinkClicked += (_, _) => ResetGestures();
        var gestureButtonsRow = Row(_addGestureButton, resetGesturesLink);
        gestureButtonsRow.Margin = new Padding(0, Dpi(2), 0, Dpi(2));
        _gestureMessage.Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(2));
        _gestureHint.Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(2));
        var gesturesSpacer = new Panel { Height = Dpi(10), Width = 1, Margin = Padding.Empty };
        RebuildGestureRows();

        var exitButton = new Button { Text = "Выйти из программы", AutoSize = true, Padding = new Padding(Dpi(6), Dpi(2), Dpi(6), Dpi(2)) };
        exitButton.Click += (_, _) => ExitFromButton();
        var hideButton = new Button { Text = "Свернуть в трей", AutoSize = true, Padding = new Padding(Dpi(6), Dpi(2), Dpi(6), Dpi(2)) };
        hideButton.Click += (_, _) => Close();
        var buttonsRow = Row(exitButton, hideButton);
        buttonsRow.FlowDirection = FlowDirection.RightToLeft;
        buttonsRow.Anchor = AnchorStyles.Right;

        layout.Controls.AddRange([titleRow, description, _statusLabel, _enabledCheckBox, _autostartCheckBox,
            _restoreVolumeCheckBox, _notificationsCheckBox, timeoutRow, gesturesHeading, _gesturesTable,
            gestureButtonsRow, _gestureMessage, _gestureHint, gesturesSpacer, buttonsRow]);
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
            _toolTip.Dispose();
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

    private void RebuildGestureRows()
    {
        _gesturesTable.SuspendLayout();
        foreach (var control in _gesturesTable.Controls.Cast<Control>().ToList()) control.Dispose();
        _gesturesTable.RowStyles.Clear();
        _gesturesTable.RowCount = Math.Max(1, Settings.Gestures.Count);

        if (Settings.Gestures.Count == 0)
        {
            var empty = new Label { Text = "Жестов нет — добавьте первый.", AutoSize = true, ForeColor = SystemColors.GrayText };
            _gesturesTable.Controls.Add(empty, 0, 0);
            _gesturesTable.SetColumnSpan(empty, 4);
        }

        for (var row = 0; row < Settings.Gestures.Count; row++)
        {
            var binding = Settings.Gestures[row];

            var sequenceBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = Dpi(110), Anchor = AnchorStyles.Left };
            sequenceBox.Items.AddRange(GestureSequence.All.Select(sequence => new SequenceItem(sequence)).ToArray<object>());
            sequenceBox.SelectedIndex = IndexOfSequence(binding.Sequence);
            sequenceBox.SelectionChangeCommitted += (_, _) => ChangeSequence(binding, sequenceBox);

            var arrow = new Label { Text = "→", AutoSize = true, Anchor = AnchorStyles.None, ForeColor = SystemColors.GrayText };

            var actionBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = Dpi(210), Anchor = AnchorStyles.Left };
            actionBox.Items.AddRange(GestureActions.All.Select(action => new ActionItem(action)).ToArray<object>());
            actionBox.SelectedIndex = Array.IndexOf(GestureActions.All, binding.Action);
            actionBox.SelectionChangeCommitted += (_, _) =>
            {
                binding.Action = ((ActionItem)actionBox.SelectedItem!).Action;
                _applySettings();
            };

            var removeButton = new Button { Text = "✕", Size = new Size(Dpi(30), sequenceBox.Height + Dpi(2)), Anchor = AnchorStyles.None };
            _toolTip.SetToolTip(removeButton, "Удалить жест");
            removeButton.Click += (_, _) =>
            {
                Settings.Gestures.Remove(binding);
                _applySettings();
                // Кнопка удаляет саму себя — перестраиваем таблицу после выхода из обработчика.
                BeginInvoke(RebuildGestureRows);
            };

            _gesturesTable.Controls.Add(sequenceBox, 0, row);
            _gesturesTable.Controls.Add(arrow, 1, row);
            _gesturesTable.Controls.Add(actionBox, 2, row);
            _gesturesTable.Controls.Add(removeButton, 3, row);
        }

        _gesturesTable.ResumeLayout();
        _addGestureButton.Enabled = Settings.Gestures.Count < GestureSequence.All.Count;
        _gestureMessage.Visible = false;
        UpdateGestureHint();
    }

    private void ChangeSequence(GestureBinding binding, ComboBox sequenceBox)
    {
        var sequence = ((SequenceItem)sequenceBox.SelectedItem!).Sequence;
        if (Settings.Gestures.Any(other => other != binding && other.Sequence == sequence))
        {
            sequenceBox.SelectedIndex = IndexOfSequence(binding.Sequence);
            _gestureMessage.Text = $"Комбинация «{GestureSequence.Display(sequence)}» уже назначена.";
            _gestureMessage.Visible = true;
            return;
        }
        binding.Sequence = sequence;
        _gestureMessage.Visible = false;
        UpdateGestureHint();
        _applySettings();
    }

    private void AddGesture()
    {
        var sequence = GestureSequence.All.FirstOrDefault(candidate => Settings.Gestures.All(g => g.Sequence != candidate));
        if (sequence is null) return;
        var action = GestureActions.All.FirstOrDefault(candidate => Settings.Gestures.All(g => g.Action != candidate), GestureAction.PlayPause);
        Settings.Gestures.Add(new GestureBinding(sequence, action));
        _applySettings();
        RebuildGestureRows();
    }

    private void ResetGestures()
    {
        Settings.Gestures = AppSettings.DefaultGestures();
        _applySettings();
        RebuildGestureRows();
    }

    private void UpdateGestureHint()
    {
        var gestures = Settings.Gestures;
        _gestureHint.Visible = gestures.Any(shorter => gestures.Any(longer =>
            longer != shorter && longer.Sequence.StartsWith(shorter.Sequence, StringComparison.Ordinal)));
    }

    private static int IndexOfSequence(string sequence) => GestureSequence.All.ToList().IndexOf(sequence);

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

    private sealed record SequenceItem(string Sequence)
    {
        public override string ToString() => GestureSequence.Display(Sequence);
    }

    private sealed record ActionItem(GestureAction Action)
    {
        public override string ToString() => GestureActions.DisplayName(Action);
    }

    private int Dpi(int value) => LogicalToDeviceUnits(value);

    private void ExitFromButton()
    {
        _allowClose = true;
        _exitApplication();
    }
}
