using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VolKey.Services;

internal sealed class KeyboardHook : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int VkVolumeDown = 0xAE;
    private const int VkVolumeUp = 0xAF;
    private IntPtr _hookId;
    private HookProc? _callback;
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
        if (code >= 0 && ((int)wParam == WmKeyDown || (int)wParam == WmSysKeyDown))
        {
            var keyCode = Marshal.ReadInt32(lParam);
            if (keyCode == VkVolumeUp) VolumeKeyPressed?.Invoke(this, VolumeKey.Up);
            if (keyCode == VkVolumeDown) VolumeKeyPressed?.Invoke(this, VolumeKey.Down);
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
