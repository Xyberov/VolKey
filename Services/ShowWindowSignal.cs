using System.Runtime.InteropServices;

namespace VolKey.Services;

/// <summary>Позволяет повторному запуску VolKey открыть окно уже работающей копии.</summary>
internal sealed class ShowWindowSignal : IDisposable
{
    private const string EventName = "VolKey.ShowWindow";
    private const int AsfwAny = -1;
    private readonly EventWaitHandle _event = new(false, EventResetMode.AutoReset, EventName);
    private readonly RegisteredWaitHandle _registration;

    /// <param name="onSignal">Вызывается в потоке пула.</param>
    public ShowWindowSignal(Action onSignal)
    {
        _registration = ThreadPool.RegisterWaitForSingleObject(
            _event, (_, _) => onSignal(), null, Timeout.Infinite, executeOnlyOnce: false);
    }

    public static void Send()
    {
        if (!EventWaitHandle.TryOpenExisting(EventName, out var handle)) return;
        using (handle)
        {
            // Повторный запуск — активный процесс; разрешаем первой копии вывести окно на передний план.
            AllowSetForegroundWindow(AsfwAny);
            handle.Set();
        }
    }

    public void Dispose()
    {
        _registration.Unregister(null);
        _event.Dispose();
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AllowSetForegroundWindow(int processId);
}
