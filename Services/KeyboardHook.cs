using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VolKey.Services;

/// <summary>
/// Глобальный хук кнопок громкости. Обработчик <see cref="VolumeKeyPressed"/> вызывается прямо из хука,
/// поэтому должен работать быстро: Windows молча снимает хук, если он отвечает слишком долго.
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;
    private const int VkVolumeDown = 0xAE;
    private const int VkVolumeUp = 0xAF;
    private IntPtr _hookId;
    private HookProc? _callback;
    private VolumeKey? _heldKey;
    public event EventHandler<VolumeKey>? VolumeKeyPressed;

    public void Start()
    {
        _callback = HookCallback;
        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hookId = SetWindowsHookEx(WhKeyboardLl, _callback, GetModuleHandle(module.ModuleName), 0);
        if (_hookId == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
    }

    private IntPtr HookCallback(int code, IntPtr wParam, IntPtr lParam)
    {
        if (code >= 0)
        {
            var key = Marshal.ReadInt32(lParam) switch
            {
                VkVolumeUp => VolumeKey.Up,
                VkVolumeDown => VolumeKey.Down,
                _ => (VolumeKey?)null
            };
            if (key.HasValue)
            {
                var message = (int)wParam;
                if (message is WmKeyDown or WmSysKeyDown)
                {
                    // Удержание кнопки шлёт повторные WM_KEYDOWN — считаем его одним нажатием.
                    if (_heldKey != key)
                    {
                        _heldKey = key;
                        VolumeKeyPressed?.Invoke(this, key.Value);
                    }
                }
                else if (message is WmKeyUp or WmSysKeyUp && _heldKey == key)
                {
                    _heldKey = null;
                }
            }
        }
        return CallNextHookEx(_hookId, code, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hookId != IntPtr.Zero) UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    private delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc callback, IntPtr hMod, uint threadId);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr GetModuleHandle(string? moduleName);
}
