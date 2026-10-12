using VolKey.Services;

namespace VolKey;

internal static class Program
{
    // Это же имя указано в AppMutex установщика, чтобы он не обновлял запущенную программу.
    private const string SingleInstanceMutexName = "VolKey.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            ShowWindowSignal.Send();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetColorMode(SystemColorMode.System);
        var startMinimized = args.Contains(AutoStartService.MinimizedArgument, StringComparer.OrdinalIgnoreCase);
        Application.Run(new VolKeyApplicationContext(startMinimized));
    }
}
