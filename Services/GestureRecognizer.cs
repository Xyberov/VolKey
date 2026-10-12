namespace VolKey.Services;

internal sealed class GestureRecognizedEventArgs(GestureAction action, VolumeState? volumeBefore) : EventArgs
{
    public GestureAction Action { get; } = action;

    /// <summary>Громкость до первого нажатия жеста, если её удалось прочитать.</summary>
    public VolumeState? VolumeBefore { get; } = volumeBefore;
}

internal sealed class GestureRecognizer : IDisposable
{
    private readonly List<(VolumeKey Key, VolumeState? VolumeBefore)> _presses = [];
    private readonly System.Windows.Forms.Timer _timer = new();
    private IReadOnlyList<CompiledGesture> _gestures = [];
    private int _maxLength = 1;
    private (GestureAction Action, VolumeState? VolumeBefore)? _pending;
    public event EventHandler<GestureRecognizedEventArgs>? GestureRecognized;

    /// <summary>Максимальная пауза между соседними нажатиями жеста.</summary>
    public int TimeoutMs { get => _timer.Interval; set => _timer.Interval = value; }

    public GestureRecognizer(int timeoutMs, IEnumerable<GestureBinding> gestures)
    {
        _timer.Interval = timeoutMs;
        _timer.Tick += (_, _) => OnTimeout();
        SetGestures(gestures);
    }

    public void SetGestures(IEnumerable<GestureBinding> gestures)
    {
        _gestures = GestureMatcher.Compile(gestures);
        _maxLength = Math.Max(1, _gestures.Select(gesture => gesture.Keys.Length).DefaultIfEmpty().Max());
        Reset();
    }

    public void Register(VolumeKey key, VolumeState? volumeBefore)
    {
        _timer.Stop();
        _pending = null;
        _presses.Add((key, volumeBefore));
        // Скользящее окно: при «+ + − +» жест «+ − +» найдётся в последних нажатиях.
        if (_presses.Count > _maxLength) _presses.RemoveAt(0);

        var evaluation = GestureMatcher.Evaluate(_presses.Select(press => press.Key).ToList(), _gestures);
        if (evaluation.Match is { } match)
        {
            var volume = _presses[^match.Keys.Length].VolumeBefore;
            if (!evaluation.CanContinue)
            {
                _presses.Clear();
                GestureRecognized?.Invoke(this, new GestureRecognizedEventArgs(match.Action, volume));
                return;
            }
            _pending = (match.Action, volume);
        }
        _timer.Start();
    }

    public void Reset()
    {
        _timer.Stop();
        _presses.Clear();
        _pending = null;
    }

    private void OnTimeout()
    {
        var pending = _pending;
        Reset();
        if (pending is { } gesture)
            GestureRecognized?.Invoke(this, new GestureRecognizedEventArgs(gesture.Action, gesture.VolumeBefore));
    }

    public void Dispose() => _timer.Dispose();
}
