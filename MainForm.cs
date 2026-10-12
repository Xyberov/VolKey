using VolKey.Services;

namespace VolKey;

internal sealed class MainForm : Form
{
    private const string RecordGlyph = "●";
    private const string StopGlyph = "■";

    private readonly GestureRecorder _recorder;
    private readonly Action _toggleEnabled;
    private readonly Action _applySettings;
    private readonly Action _exitApplication;
    private readonly Label _descriptionLabel = new() { AutoSize = true };
    private readonly Label _statusLabel = new() { AutoSize = true };
    private readonly CheckBox _enabledCheckBox = new() { AutoSize = true };
    private readonly CheckBox _autostartCheckBox = new() { AutoSize = true };
    private readonly CheckBox _restoreVolumeCheckBox = new() { AutoSize = true };
    private readonly CheckBox _notificationsCheckBox = new() { AutoSize = true };
    private readonly Label _timeoutLabel = new() { AutoSize = true };
    private readonly NumericUpDown _timeoutInput = new();
    private readonly Label _languageLabel = new() { AutoSize = true };
    private readonly ComboBox _languageBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _gesturesHeading = new() { AutoSize = true };
    private readonly TableLayoutPanel _gesturesTable = new() { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 5 };
    private readonly Button _addGestureButton = new() { AutoSize = true };
    private readonly Button _recordGestureButton = new() { AutoSize = true };
    private readonly LinkLabel _resetGesturesLink = new() { AutoSize = true };
    private readonly Label _gestureMessage = new() { AutoSize = true, Visible = false };
    private readonly Label _gestureHint = new() { AutoSize = true, ForeColor = SystemColors.GrayText, Visible = false };
    private readonly Button _hideButton = new() { AutoSize = true };
    private readonly Button _quitButton = new() { AutoSize = true };
    private readonly ToolStripMenuItem _openMenuItem = new();
    private readonly ToolStripMenuItem _toggleMenuItem = new();
    private readonly ToolStripMenuItem _quitMenuItem = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ToolTip _toolTip = new();
    private readonly Icon _enabledTrayIcon = AppIcons.Load(SystemInformation.SmallIconSize);
    private readonly Icon _disabledTrayIcon;
    private Button? _recordingButton;
    private GestureBinding? _recordingTarget;
    private bool _allowClose;
    private bool _isInitializing = true;
    private bool _hiddenHintShown;

    public AppSettings Settings { get; }

    public MainForm(AppSettings settings, GestureRecorder recorder, Action toggleEnabled, Action applySettings, Action exitApplication)
    {
        Settings = settings;
        _recorder = recorder;
        _toggleEnabled = toggleEnabled;
        _applySettings = applySettings;
        _exitApplication = exitApplication;
        _disabledTrayIcon = AppIcons.CreateDisabled(_enabledTrayIcon);
        _recorder.Recorded += OnGestureRecorded;

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
        _descriptionLabel.Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(8));
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
        _timeoutLabel.Margin = new Padding(Dpi(3), Dpi(6), Dpi(6), 0);
        var timeoutRow = Row(_timeoutLabel, _timeoutInput);
        timeoutRow.Margin = new Padding(0, Dpi(4), 0, Dpi(2));

        _languageBox.Width = Dpi(150);
        _languageBox.SelectionChangeCommitted += (_, _) => ChangeLanguage(((LanguageItem)_languageBox.SelectedItem!).Language);
        _languageLabel.Margin = new Padding(Dpi(3), Dpi(6), Dpi(6), 0);
        var languageRow = Row(_languageLabel, _languageBox);
        languageRow.Margin = new Padding(0, Dpi(2), 0, Dpi(12));

        _gesturesHeading.Font = new Font(Font, FontStyle.Bold);
        _gesturesHeading.Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(4));
        _gesturesTable.Margin = Padding.Empty;
        _addGestureButton.Click += (_, _) => AddGesture();
        _recordGestureButton.ForeColor = Color.Firebrick;
        _recordGestureButton.Click += (_, _) => ToggleRecording(null, _recordGestureButton);
        _resetGesturesLink.Margin = new Padding(Dpi(10), Dpi(8), 0, 0);
        _resetGesturesLink.LinkClicked += (_, _) => ResetGestures();
        var gestureButtonsRow = Row(_addGestureButton, _recordGestureButton, _resetGesturesLink);
        gestureButtonsRow.Margin = new Padding(0, Dpi(2), 0, Dpi(2));
        _gestureMessage.Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(2));
        _gestureHint.Margin = new Padding(Dpi(3), 0, Dpi(3), Dpi(2));
        var gesturesSpacer = new Panel { Height = Dpi(10), Width = 1, Margin = Padding.Empty };

        _quitButton.Padding = new Padding(Dpi(6), Dpi(2), Dpi(6), Dpi(2));
        _quitButton.Click += (_, _) => ExitFromButton();
        _hideButton.Padding = new Padding(Dpi(6), Dpi(2), Dpi(6), Dpi(2));
        _hideButton.Click += (_, _) => Close();
        var buttonsRow = Row(_quitButton, _hideButton);
        buttonsRow.FlowDirection = FlowDirection.RightToLeft;
        buttonsRow.Anchor = AnchorStyles.Right;

        layout.Controls.AddRange([titleRow, _descriptionLabel, _statusLabel, _enabledCheckBox, _autostartCheckBox,
            _restoreVolumeCheckBox, _notificationsCheckBox, timeoutRow, languageRow, _gesturesHeading, _gesturesTable,
            gestureButtonsRow, _gestureMessage, _gestureHint, gesturesSpacer, buttonsRow]);
        Controls.Add(layout);
        CancelButton = _hideButton;

        _openMenuItem.Font = new Font(Font, FontStyle.Bold);
        _openMenuItem.Click += (_, _) => ShowWindow();
        _toggleMenuItem.Click += (_, _) => _toggleEnabled();
        _quitMenuItem.Click += (_, _) => ExitFromButton();
        var menu = new ContextMenuStrip();
        menu.Items.AddRange([_openMenuItem, _toggleMenuItem, new ToolStripSeparator(), _quitMenuItem]);

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
        ApplyTexts();
    }

    public void RefreshState()
    {
        _enabledCheckBox.Checked = Settings.Enabled;
        _statusLabel.Text = Strings.Status(Settings.Enabled);
        _statusLabel.ForeColor = Settings.Enabled ? Color.ForestGreen : Color.Firebrick;
        _toggleMenuItem.Checked = Settings.Enabled;
        _trayIcon.Icon = Settings.Enabled ? _enabledTrayIcon : _disabledTrayIcon;
        _trayIcon.Text = Strings.TrayTooltip(Settings.Enabled);
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

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        // Esc во время записи отменяет её, а не прячет окно.
        if (keyData == Keys.Escape && _recorder.IsRecording)
        {
            CancelRecording();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
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
            _recorder.Recorded -= OnGestureRecorded;
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _toolTip.Dispose();
            _enabledTrayIcon.Dispose();
            _disabledTrayIcon.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ApplyTexts()
    {
        _descriptionLabel.Text = Strings.Description;
        _enabledCheckBox.Text = Strings.EnableGestures;
        _autostartCheckBox.Text = Strings.StartWithWindows;
        _restoreVolumeCheckBox.Text = Strings.RestoreVolume;
        _notificationsCheckBox.Text = Strings.ShowNotifications;
        _timeoutLabel.Text = Strings.MaxPause;
        _languageLabel.Text = Strings.Language;
        _gesturesHeading.Text = Strings.Gestures;
        _addGestureButton.Text = Strings.AddGesture;
        _resetGesturesLink.Text = Strings.ResetGestures;
        _gestureHint.Text = Strings.PrefixHint;
        _hideButton.Text = Strings.HideToTray;
        _quitButton.Text = Strings.Quit;
        _openMenuItem.Text = Strings.TrayOpen;
        _toggleMenuItem.Text = Strings.TrayGesturesOn;
        _quitMenuItem.Text = Strings.TrayQuit;

        _languageBox.Items.Clear();
        _languageBox.Items.AddRange(Enum.GetValues<UiLanguage>().Select(language => new LanguageItem(language)).ToArray<object>());
        _languageBox.SelectedIndex = (int)Settings.Language;

        RefreshState();
        RebuildGestureRows();
    }

    private void ChangeLanguage(UiLanguage language)
    {
        Settings.Language = language;
        Strings.SetLanguage(language);
        _applySettings();
        ApplyTexts();
    }

    private void HideToTray()
    {
        CancelRecording();
        Hide();
        if (_hiddenHintShown) return;
        _hiddenHintShown = true;
        _trayIcon.ShowBalloonTip(1200, "VolKey", Strings.StillRunning, ToolTipIcon.Info);
    }

    private void RebuildGestureRows()
    {
        CancelRecording();
        _gesturesTable.SuspendLayout();
        foreach (var control in _gesturesTable.Controls.Cast<Control>().ToList()) control.Dispose();
        _gesturesTable.RowStyles.Clear();
        _gesturesTable.RowCount = Math.Max(1, Settings.Gestures.Count);

        if (Settings.Gestures.Count == 0)
        {
            var empty = new Label { Text = Strings.NoGestures, AutoSize = true, ForeColor = SystemColors.GrayText };
            _gesturesTable.Controls.Add(empty, 0, 0);
            _gesturesTable.SetColumnSpan(empty, _gesturesTable.ColumnCount);
        }

        for (var row = 0; row < Settings.Gestures.Count; row++)
        {
            var binding = Settings.Gestures[row];

            var sequenceBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = Dpi(110), Anchor = AnchorStyles.Left };
            sequenceBox.Items.AddRange(GestureSequence.All.Select(sequence => new SequenceItem(sequence)).ToArray<object>());
            sequenceBox.SelectedIndex = IndexOfSequence(binding.Sequence);
            sequenceBox.SelectionChangeCommitted += (_, _) => ChangeSequence(binding, sequenceBox);
            var rowButtonSize = new Size(Dpi(30), sequenceBox.Height + Dpi(2));

            var recordButton = new Button { Text = RecordGlyph, Size = rowButtonSize, Anchor = AnchorStyles.None, ForeColor = Color.Firebrick };
            _toolTip.SetToolTip(recordButton, Strings.RecordSequence);
            recordButton.Click += (_, _) => ToggleRecording(binding, recordButton);

            var arrow = new Label { Text = "→", AutoSize = true, Anchor = AnchorStyles.None, ForeColor = SystemColors.GrayText };

            var actionBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = Dpi(210), Anchor = AnchorStyles.Left };
            actionBox.Items.AddRange(GestureActions.All.Select(action => new ActionItem(action)).ToArray<object>());
            actionBox.SelectedIndex = Array.IndexOf(GestureActions.All, binding.Action);
            actionBox.SelectionChangeCommitted += (_, _) =>
            {
                binding.Action = ((ActionItem)actionBox.SelectedItem!).Action;
                _applySettings();
            };

            var removeButton = new Button { Text = "✕", Size = rowButtonSize, Anchor = AnchorStyles.None };
            _toolTip.SetToolTip(removeButton, Strings.RemoveGesture);
            removeButton.Click += (_, _) =>
            {
                Settings.Gestures.Remove(binding);
                _applySettings();
                // Кнопка удаляет саму себя — перестраиваем таблицу после выхода из обработчика.
                BeginInvoke(RebuildGestureRows);
            };

            _gesturesTable.Controls.Add(sequenceBox, 0, row);
            _gesturesTable.Controls.Add(recordButton, 1, row);
            _gesturesTable.Controls.Add(arrow, 2, row);
            _gesturesTable.Controls.Add(actionBox, 3, row);
            _gesturesTable.Controls.Add(removeButton, 4, row);
        }

        _gesturesTable.ResumeLayout();
        var canAdd = Settings.Gestures.Count < GestureSequence.All.Count;
        _addGestureButton.Enabled = canAdd;
        _recordGestureButton.Enabled = canAdd;
        HideGestureMessage();
        UpdateGestureHint();
    }

    private void ChangeSequence(GestureBinding binding, ComboBox sequenceBox)
    {
        var sequence = ((SequenceItem)sequenceBox.SelectedItem!).Sequence;
        if (IsAssignedToOther(sequence, binding))
        {
            sequenceBox.SelectedIndex = IndexOfSequence(binding.Sequence);
            ShowGestureMessage(Strings.AlreadyAssigned(GestureSequence.Display(sequence)), Color.Firebrick);
            return;
        }
        binding.Sequence = sequence;
        HideGestureMessage();
        UpdateGestureHint();
        _applySettings();
    }

    /// <param name="target">Жест, которому записывается комбинация; null — записать новый жест.</param>
    private void ToggleRecording(GestureBinding? target, Button button)
    {
        var wasThisButton = _recordingButton == button;
        CancelRecording();
        if (wasThisButton) return;

        _recordingTarget = target;
        _recordingButton = button;
        button.Text = target is null ? $"{StopGlyph}  {Strings.StopRecording}" : StopGlyph;
        _toolTip.SetToolTip(button, Strings.StopRecording);
        ShowGestureMessage(Strings.RecordingPrompt, SystemColors.HotTrack);
        _recorder.Start(Settings.GestureTimeoutMs);
    }

    private void CancelRecording()
    {
        _recorder.Cancel();
        ResetRecordingButton();
        if (_gestureMessage.Text == Strings.RecordingPrompt) HideGestureMessage();
    }

    private void ResetRecordingButton()
    {
        if (_recordingButton is { IsDisposed: false } button && button != _recordGestureButton)
        {
            button.Text = RecordGlyph;
            _toolTip.SetToolTip(button, Strings.RecordSequence);
        }
        _recordGestureButton.Text = $"{RecordGlyph}  {Strings.RecordGesture}";
        _toolTip.SetToolTip(_recordGestureButton, null);
        _recordingButton = null;
        _recordingTarget = null;
    }

    private void OnGestureRecorded(object? sender, GestureRecordedEventArgs e)
    {
        var target = _recordingTarget;
        ResetRecordingButton();
        var display = GestureSequence.Display(e.Sequence);

        if (!GestureSequence.IsValid(e.Sequence))
        {
            ShowGestureMessage(Strings.InvalidRecording, Color.Firebrick);
            return;
        }
        if (IsAssignedToOther(e.Sequence, target))
        {
            ShowGestureMessage(Strings.AlreadyAssigned(display), Color.Firebrick);
            return;
        }

        if (target is null)
            Settings.Gestures.Add(new GestureBinding(e.Sequence, NextFreeAction()));
        else
            target.Sequence = e.Sequence;
        _applySettings();
        RebuildGestureRows();
        ShowGestureMessage(Strings.Recorded(display), Color.ForestGreen);
    }

    private void AddGesture()
    {
        var sequence = GestureSequence.All.FirstOrDefault(candidate => !IsAssignedToOther(candidate, null));
        if (sequence is null) return;
        Settings.Gestures.Add(new GestureBinding(sequence, NextFreeAction()));
        _applySettings();
        RebuildGestureRows();
    }

    private void ResetGestures()
    {
        Settings.Gestures = AppSettings.DefaultGestures();
        _applySettings();
        RebuildGestureRows();
    }

    private GestureAction NextFreeAction() =>
        GestureActions.All.FirstOrDefault(candidate => Settings.Gestures.All(g => g.Action != candidate), GestureAction.PlayPause);

    private bool IsAssignedToOther(string sequence, GestureBinding? binding) =>
        Settings.Gestures.Any(other => other != binding && other.Sequence == sequence);

    private void ShowGestureMessage(string text, Color color)
    {
        _gestureMessage.Text = text;
        _gestureMessage.ForeColor = color;
        _gestureMessage.Visible = true;
    }

    private void HideGestureMessage()
    {
        _gestureMessage.Visible = false;
        _gestureMessage.Text = "";
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
        public override string ToString() => Strings.Action(Action);
    }

    private sealed record LanguageItem(UiLanguage Language)
    {
        public override string ToString() => Strings.LanguageName(Language);
    }

    private int Dpi(int value) => LogicalToDeviceUnits(value);

    private void ExitFromButton()
    {
        _allowClose = true;
        _exitApplication();
    }
}
