namespace VolKey.Services;

internal sealed class GestureRecordedEventArgs(string sequence, VolumeState? volumeBefore) : EventArgs
{
    /// <summary>Записанные нажатия в виде строки из «+» и «-». Может оказаться недопустимой комбинацией.</summary>
    public string Sequence { get; } = sequence;

    public VolumeState? VolumeBefore { get; } = volumeBefore;
}

/// <summary>Записывает комбинацию с кнопок громкости: запись заканчивается паузой или на последнем допустимом нажатии.</summary>
internal sealed class GestureRecorder : IDisposable
{
    private readonly List<VolumeKey> _keys = [];
    private readonly System.Windows.Forms.Timer _timer = new();
    private VolumeState? _volumeBefore;
    public event EventHandler<GestureRecordedEventArgs>? Recorded;

    public bool IsRecording { get; private set; }

    public GestureRecorder()
    {
        _timer.Tick += (_, _) => Finish();
    }

    /// <param name="pauseMs">Пауза после последнего нажатия, которая завершает запись.</param>
    public void Start(int pauseMs)
    {
        Cancel();
        _timer.Interval = pauseMs;
        IsRecording = true;
    }

    public void Register(VolumeKey key, VolumeState? volumeBefore)
    {
        if (!IsRecording) return;
        _timer.Stop();
        if (_keys.Count == 0) _volumeBefore = volumeBefore;
        _keys.Add(key);
        if (_keys.Count >= GestureSequence.MaxLength)
            Finish();
        else
            _timer.Start();
    }

    public void Cancel()
    {
        _timer.Stop();
        _keys.Clear();
        _volumeBefore = null;
        IsRecording = false;
    }

    private void Finish()
    {
        var sequence = new string(_keys.Select(key => key == VolumeKey.Up ? '+' : '-').ToArray());
        var volumeBefore = _volumeBefore;
        Cancel();
        Recorded?.Invoke(this, new GestureRecordedEventArgs(sequence, volumeBefore));
    }

    public void Dispose() => _timer.Dispose();
}
