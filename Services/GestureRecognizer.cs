namespace VolKey.Services;

internal enum VolumeKey { Up, Down }
internal enum MediaCommand { Next, Previous }

internal sealed class GestureRecognizer : IDisposable
{
    private readonly List<VolumeKey> _keys = [];
    private readonly System.Windows.Forms.Timer _timer = new();
    public int TimeoutMs { get => _timer.Interval; set => _timer.Interval = value; }
    public event EventHandler<MediaCommand>? GestureRecognized;

    public GestureRecognizer(int timeoutMs)
    {
        _timer.Interval = timeoutMs;
        _timer.Tick += (_, _) => Reset();
    }

    public void Register(VolumeKey key)
    {
        _timer.Stop();
        _keys.Add(key);
        if (_keys.Count == 3)
        {
            var command = _keys switch
            {
                [VolumeKey.Up, VolumeKey.Down, VolumeKey.Up] => MediaCommand.Next,
                [VolumeKey.Down, VolumeKey.Up, VolumeKey.Down] => MediaCommand.Previous,
                _ => (MediaCommand?)null
            };
            Reset();
            if (command.HasValue) GestureRecognized?.Invoke(this, command.Value);
            return;
        }
        _timer.Start();
    }

    private void Reset()
    {
        _timer.Stop();
        _keys.Clear();
    }

    public void Dispose() => _timer.Dispose();
}
