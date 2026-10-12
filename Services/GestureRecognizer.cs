namespace VolKey.Services;

internal enum VolumeKey { Up, Down }
internal enum MediaCommand { Next, Previous }

internal sealed class GestureRecognizedEventArgs(MediaCommand command, VolumeState? volumeBefore) : EventArgs
{
    public MediaCommand Command { get; } = command;

    /// <summary>Громкость до первого нажатия жеста, если её удалось прочитать.</summary>
    public VolumeState? VolumeBefore { get; } = volumeBefore;
}

internal sealed class GestureRecognizer : IDisposable
{
    private const int GestureLength = 3;
    private readonly List<(VolumeKey Key, VolumeState? VolumeBefore)> _presses = [];
    private readonly System.Windows.Forms.Timer _timer = new();
    public event EventHandler<GestureRecognizedEventArgs>? GestureRecognized;

    /// <summary>Максимальная пауза между соседними нажатиями жеста.</summary>
    public int TimeoutMs { get => _timer.Interval; set => _timer.Interval = value; }

    public GestureRecognizer(int timeoutMs)
    {
        _timer.Interval = timeoutMs;
        _timer.Tick += (_, _) => Reset();
    }

    public void Register(VolumeKey key, VolumeState? volumeBefore)
    {
        _timer.Stop();
        _presses.Add((key, volumeBefore));
        // Скользящее окно: при «+ + − +» жест найдётся в последних трёх нажатиях.
        if (_presses.Count > GestureLength) _presses.RemoveAt(0);

        if (_presses.Count == GestureLength && Match() is { } command)
        {
            var volume = _presses[0].VolumeBefore;
            _presses.Clear();
            GestureRecognized?.Invoke(this, new GestureRecognizedEventArgs(command, volume));
            return;
        }
        _timer.Start();
    }

    public void Reset()
    {
        _timer.Stop();
        _presses.Clear();
    }

    private MediaCommand? Match() => (_presses[0].Key, _presses[1].Key, _presses[2].Key) switch
    {
        (VolumeKey.Up, VolumeKey.Down, VolumeKey.Up) => MediaCommand.Next,
        (VolumeKey.Down, VolumeKey.Up, VolumeKey.Down) => MediaCommand.Previous,
        _ => null
    };

    public void Dispose() => _timer.Dispose();
}
